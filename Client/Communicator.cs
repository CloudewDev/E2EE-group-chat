using HandshakeStateMachine_ns;
using JsonController_ns;
using ServerConnector_ns;
using Ratchet_ns;

using System;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using System.Runtime.CompilerServices;

namespace Communicator_ns
{
    public class Communicator
    {
        protected readonly ServerConnector_ns.ServerConnector server_connector;
        protected readonly HandshakeStateMachine_ns.HandshakeStateMachine handshake_state_machine; // must controlled with semaphore since this is shared resource
        protected readonly JsonController_ns.JsonController json_controller;
        protected readonly Ratchet_ns.Ratchet ratchet; // must controlled with semaphore since this is shared resource
        protected readonly Socket sock;
        protected readonly string nickname;
        protected readonly SemaphoreSlim state_semaphore;
        protected readonly SemaphoreSlim ratchet_semaphore;
        protected Channel<SEND_TYPE> dh_exchange_queue;
        protected Channel<SEND_TYPE> key_exchange_queue;
        //dependancy injections

        public enum SEND_TYPE { rcv_first, send_first }
        public Communicator(ServerConnector sc,
            HandshakeStateMachine hs,
            JsonController jp,
            Ratchet rc,
            string nickname,
            SemaphoreSlim ss,
            SemaphoreSlim rs,
            Channel<SEND_TYPE> deq,
            Channel<SEND_TYPE> keq)
        {
            server_connector = sc;
            handshake_state_machine = hs;
            sock = server_connector.Sock;
            json_controller = jp;
            ratchet = rc;
            this.nickname = nickname;
            state_semaphore = ss;
            ratchet_semaphore = rs;
            dh_exchange_queue = deq;
            this.key_exchange_queue = keq;
        }
    }

    public class Reciever : Communicator
    {
        public Reciever(ServerConnector_ns.ServerConnector sc,
            HandshakeStateMachine_ns.HandshakeStateMachine hs,
            JsonController_ns.JsonController jp,
            Ratchet rc,
            string nickname,
            SemaphoreSlim ss,
            SemaphoreSlim rs,
            Channel<SEND_TYPE> deq,
            Channel<SEND_TYPE> keq) : base(sc, hs, jp, rc, nickname, ss, rs, deq, keq)
        { }

        public async Task LoopRecieveAsync(CancellationToken token)
        {

            while (!token.IsCancellationRequested)
            {
                string message = await RecieveAsync(token);
                Console.WriteLine("[log]recieved :" + message);

                switch (json_controller.ParseTypeFromJson(message))
                {
                    case (int)JsonController.MSG_TYPE.message:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(
                                    json_controller.ParseFromFromJson(message) //nickname
                                    + " : "
                                    + Encoding.UTF8.GetString(Decrypt(ratchet.AccessData(json_controller.ParseFromFromJson(message)).sender_key,
                                        Convert.FromBase64String(json_controller.ParseIVFromJson(message)),
                                        Convert.FromBase64String(json_controller.ParseBodyFromJson(message))) // decrypt and encode recieved message
                                     )
                        );
                        Console.ResetColor();
                        break;
                    case (int)JsonController.MSG_TYPE.dh:
                        //what did I recieved? response of handshake or handshake request?
                        if (handshake_state_machine.MyState == HandshakeStateMachine.STATE.waiting_for_rcv_dh)
                        {
                            //which means I was waiting for response of handshake
                            //check if current message is from current handshaker
                            if (json_controller.ParseFromFromJson(message) == handshake_state_machine.CurrentHandShaker)
                            {
                                handshake_state_machine.OpponentBigNum = json_controller.ParseBodyFromJson(message);
                                await state_semaphore.WaitAsync(token);
                                try
                                {
                                    handshake_state_machine.SetMyState_ToRcvedDH();
                                }
                                finally { state_semaphore.Release(); }
                            }
                            else
                            {
                                Console.WriteLine("[log]ignore current message");
                            }
                        }
                        else if (handshake_state_machine.MyState == HandshakeStateMachine.STATE.idle)
                        {
                            //which means I suddenly recieved handshake request
                            handshake_state_machine.OpponentBigNum = json_controller.ParseBodyFromJson(message);
                            await state_semaphore.WaitAsync(token);
                            try
                            {
                                handshake_state_machine.CurrentHandShaker = json_controller.ParseFromFromJson(message);
                                handshake_state_machine.SetMyState_ToSendDH();
                            }
                            finally { state_semaphore.Release(); }
                            await dh_exchange_queue.Writer.WriteAsync(SEND_TYPE.rcv_first); // wake up the DH handshake sender
                        }
                        break;
                    case (int)JsonController.MSG_TYPE.sender_key:
                        //sender key is encrypted by session key. so I need to decrypt first.
                        await ratchet_semaphore.WaitAsync(token);
                        try
                        {
                            ratchet.CurrentExchanger = json_controller.ParseFromFromJson(message);
                            ratchet.SetOpponentSenderKey // this function automatically decyrpt the input and set the opponent's sender key
                                (
                                Convert.FromBase64String(json_controller.ParseIVFromJson(message)),
                                Convert.FromBase64String(json_controller.ParseBodyFromJson(message))
                                );
                            if (ratchet.MyState == Ratchet.STATE.waiting_for_rcv_key) // check if this sender key is as response of my sender key or not
                            {
                                ratchet.SetMyState_ToRcvedKey();
                            }
                            else if (ratchet.MyState == Ratchet.STATE.idle)
                            {
                                ratchet.SetMyState_ToSendKey();
                            }

                        }
                        finally { ratchet_semaphore.Release(); }
                        await key_exchange_queue.Writer.WriteAsync(SEND_TYPE.rcv_first); //wake up the sender key handshake sender
                        break;
                    case (int)JsonController.MSG_TYPE.enter:
                        //someone entered. I must DH handshake to this new user.
                        if (handshake_state_machine.MyState == HandshakeStateMachine.STATE.idle) // but not when I'm handshaking.
                        {
                            //session key is 64byte and first 32 bytes are proper key (other 32 bytes are root key)
                            byte[] enter_key = new byte[32];
                            Array.Copy(ratchet.AccessData("server").session_key, 0, enter_key, 0, 32); 
                            //this type of message is only from server. so check server's session key
                            
                            byte[] enter_decrypted_body = Decrypt(
                                enter_key,
                                Convert.FromBase64String(json_controller.ParseIVFromJson(message)),
                                Convert.FromBase64String(json_controller.ParseBodyFromJson(message))
                                );
                            string enter_msg_body = Encoding.UTF8.GetString(enter_decrypted_body);
                            
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[alert]" + enter_msg_body + " entered");
                            Console.ResetColor();
                            
                            await state_semaphore.WaitAsync(token);
                            try
                            {
                                handshake_state_machine.CurrentHandShaker = enter_msg_body;
                                handshake_state_machine.SetMyState_ToSendDH();
                            }
                            finally { state_semaphore.Release(); }
                            await dh_exchange_queue.Writer.WriteAsync(SEND_TYPE.send_first);
                        }
                        else
                        {
                            Console.WriteLine("[log]ignore current message");
                        }
                        break;
                    case (int)JsonController.MSG_TYPE.leave:
                        //similar to the process of someone's enter
                        byte[] leave_current_key = new byte[32];
                        Array.Copy(ratchet.AccessData("server").session_key, 0, leave_current_key, 0, 32);
                        byte[] leave_decrypted_body = Decrypt(
                            leave_current_key,
                            Convert.FromBase64String(json_controller.ParseIVFromJson(message)),
                            Convert.FromBase64String(json_controller.ParseBodyFromJson(message))
                            );

                        string exit_msg_body = Encoding.UTF8.GetString(leave_decrypted_body);
                        await ratchet_semaphore.WaitAsync(token);
                        try
                        {
                            ratchet.RemoveData(exit_msg_body);
                        }
                        finally { ratchet_semaphore.Release(); }
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("[alert]" + exit_msg_body + " leaved");
                        Console.ResetColor();
                        break;
                }

            }

        }
        private async Task<string> RecieveAsync(CancellationToken token)
        {
            byte[] msg_size_data = new byte[4];
            string? message;
            await ReadNBytesAsync(4, msg_size_data, token);
            Array.Reverse(msg_size_data);
            int msg_size = BitConverter.ToInt32(msg_size_data, 0);
            //first 4 bytes mean the size of a message. so read those first.

            byte[] message_byte = new byte[msg_size];
            await ReadNBytesAsync(msg_size, message_byte, token); // ReadNBytesAsync function already has exception handling
            message = Encoding.UTF8.GetString(message_byte, 0, msg_size);
            return message;
        }
        private async Task ReadNBytesAsync(int n, byte[] buffer, CancellationToken token)
        {
            int currentRead = 0;
            int bytes_read = 0; ;
            while (currentRead < n) // to accurately read n bytes
            {
                bytes_read = await sock.ReceiveAsync(new ArraySegment<byte>(buffer, currentRead, n - currentRead), SocketFlags.None, token);
                if (bytes_read <= 0)
                {
                    throw new Exception("[log]server closed");
                }
                currentRead += bytes_read;

            }

        }

        private byte[] Decrypt(byte[] key, byte[] iv, byte[] ciphertext)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        }
    }

    public class Sender : Communicator
    {
        public Sender(ServerConnector_ns.ServerConnector sc,
            HandshakeStateMachine_ns.HandshakeStateMachine hs,
            JsonController_ns.JsonController jp,
            Ratchet rc,
            string nickname,
            SemaphoreSlim ss,
            SemaphoreSlim rs,
            Channel<SEND_TYPE> deq,
            Channel<SEND_TYPE> keq) : base(sc, hs, jp, rc, nickname, ss, rs, deq, keq)
        {
            Ratchet.KEYS temp = ratchet.MakeNewKey();
            temp.sender_key = ratchet.MySenderKey;
            ratchet.AddData(nickname, temp);
        }
        //build my sender key. this is kind of initialize but not neccessary to do here. just whenever before communicate with other user.

        public async Task Init()
        {
            await dh_exchange_queue.Writer.WriteAsync(SEND_TYPE.send_first);
        }
        // since I must handshake with server first, set the initial state for start handshake with server.

        private readonly SemaphoreSlim sock_semaphore = new SemaphoreSlim(1, 1);
        public async Task LoopInputAsync(CancellationToken token) // this is function for user's I/O input
        {
            while (!token.IsCancellationRequested)
            {
                string line = await Task.Run(() => Console.ReadLine());
                if (string.IsNullOrEmpty(line)) continue;
                if (line == "/quit")
                {
                    break; //it changes the token which is input and make upper function avilable to syncronize other functions 
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(nickname + " : " + line);
                    Console.ResetColor();

                    using (RandomNumberGenerator r = RandomNumberGenerator.Create())
                    {
                        byte[] ivec = new byte[16];
                        r.GetBytes(ivec);
                        //sending message with my sender key encryption
                        line = json_controller.BuildJson
                            (
                            JsonController.MSG_TYPE.message,
                            nickname,
                            "group",
                            Convert.ToBase64String(Encrypt(ratchet.MySenderKey, ivec, Encoding.UTF8.GetBytes(line))),
                            Convert.ToBase64String(ivec)
                            );
                        Console.WriteLine("[log]sent system to :" + line);
                        await sock_semaphore.WaitAsync(token);
                        try
                        {
                            await sock.SendAsync(new ArraySegment<byte>(MakeBytesFormat(line)), SocketFlags.None);
                        }
                        finally { sock_semaphore.Release(); }

                    }
                }
            }

        }
        public async Task LoopHandshakeAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                SEND_TYPE send_type = await dh_exchange_queue.Reader.ReadAsync(token); //blocking here

                if (handshake_state_machine.MyState == HandshakeStateMachine.STATE.send_dh) // check if the state is proper or not first
                {
                    string message;
                    if (send_type == SEND_TYPE.send_first) // it means I'm starting handshake
                    {
                        message = json_controller.BuildJson
                            (JsonController.MSG_TYPE.dh, nickname, handshake_state_machine.CurrentHandShaker, handshake_state_machine.MyBigNum, "");
                        await state_semaphore.WaitAsync(token);
                        try
                        {
                            handshake_state_machine.SetMyState_ToWaitingForRcvDH();
                        }
                        finally { state_semaphore.Release(); }
                        while (handshake_state_machine.MyState != HandshakeStateMachine.STATE.rcved_dh) //repeat untill I get response
                        {
                            Console.WriteLine("[log]waiting for handshake response");
                            await sock_semaphore.WaitAsync(token);
                            try
                            {
                                await sock.SendAsync(new ArraySegment<byte>(MakeBytesFormat(message)), SocketFlags.None);
                            }
                            finally { sock_semaphore.Release(); }
                            await Task.Delay(100, token);
                        }
                        Console.WriteLine("[log]handshaker sent :" + message);

                        string current_handshaker = handshake_state_machine.CurrentHandShaker;
                        
                        await state_semaphore.WaitAsync(token);
                        try
                        {
                            handshake_state_machine.SetMyState_ToIdle();
                        }
                        finally { state_semaphore.Release(); }
                        // if I dont have sender key of current handshaker, start sender key handshake
                        if (ratchet.AccessData(current_handshaker).sender_key == null && current_handshaker != "server")
                        {
                            await ratchet_semaphore.WaitAsync(token);
                            try
                            {
                                ratchet.CurrentExchanger = current_handshaker;
                                ratchet.SetMyState_ToSendKey();
                            }
                            finally { ratchet_semaphore.Release(); }
                            await key_exchange_queue.Writer.WriteAsync(SEND_TYPE.send_first);

                        }
                    }
                    else // it means I recieved handshake request and I'm answering
                    {
                        message = json_controller.BuildJson
                           (JsonController.MSG_TYPE.dh, nickname, handshake_state_machine.CurrentHandShaker, handshake_state_machine.MyBigNum, "");
                        await state_semaphore.WaitAsync(token);
                        try
                        {
                            handshake_state_machine.SetMyState_ToIdle();
                        }
                        finally { state_semaphore.Release(); }
                        await sock_semaphore.WaitAsync(token);
                        try
                        {
                            await sock.SendAsync(new ArraySegment<byte>(MakeBytesFormat(message)), SocketFlags.None);
                        }
                        finally { sock_semaphore.Release(); }
                        Console.WriteLine("[log]handshaker sent :" + message);
                    }
                }
                else
                {
                }

            }

        }

        public async Task LoopSenderAsync(CancellationToken token) // similar to DH handshake function
        {
            while (!token.IsCancellationRequested)
            {
                SEND_TYPE send_type = await key_exchange_queue.Reader.ReadAsync(token);
                if (ratchet.MyState == Ratchet.STATE.send_key)
                {
                    string message;
                    using RandomNumberGenerator r = RandomNumberGenerator.Create();
                    byte[] ivec = new byte[16];
                    r.GetBytes(ivec);
                    byte[] current_key = new byte[32];
                    Array.Copy(ratchet.AccessData(ratchet.CurrentExchanger).session_key, 0, current_key, 0, 32);
                    message = json_controller.BuildJson(
                        JsonController.MSG_TYPE.sender_key,
                        nickname,
                        ratchet.CurrentExchanger,
                        Convert.ToBase64String(Encrypt(current_key, ivec, ratchet.MySenderKey)),
                        Convert.ToBase64String(ivec));
                    Console.WriteLine("[log]sending my sender key with session key encryption");

                    if (send_type == SEND_TYPE.send_first)
                    {
                        await ratchet_semaphore.WaitAsync(token);
                        try
                        {
                            ratchet.SetMyState_ToWaitingForRcvKey();
                        }
                        finally { ratchet_semaphore.Release(); }

                        while (ratchet.MyState != Ratchet.STATE.rcved_key)
                        {
                            await sock_semaphore.WaitAsync(token);
                            try
                            {
                                await sock.SendAsync(new ArraySegment<byte>(MakeBytesFormat(message)), SocketFlags.None);
                            }
                            finally { sock_semaphore.Release(); }
                            await Task.Delay(100, token);
                        }

                        await ratchet_semaphore.WaitAsync(token);
                        try
                        {
                            ratchet.SetMyState_ToIdle();
                        }
                        finally { ratchet_semaphore.Release(); }

                    }
                    else
                    {
                        await sock_semaphore.WaitAsync(token);
                        try
                        {
                            await sock.SendAsync(new ArraySegment<byte>(MakeBytesFormat(message)), SocketFlags.None);
                        }
                        finally { sock_semaphore.Release(); }
                        await ratchet_semaphore.WaitAsync(token);
                        try
                        {
                            ratchet.SetMyState_ToIdle();
                        }
                        finally { ratchet_semaphore.Release(); }

                    }
                    Console.WriteLine("[log]ratchet sent :" + message);

                }

            }
        }

        //measure the size of current message and add 4 bytes front of the message which means size of the message
        private byte[] MakeBytesFormat(string input)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(input);
            byte[] lengthPrefix = BitConverter.GetBytes(buffer.Length);
            byte[] packet = new byte[lengthPrefix.Length + buffer.Length];
            Buffer.BlockCopy(lengthPrefix, 0, packet, 0, 4);
            Buffer.BlockCopy(buffer, 0, packet, 4, buffer.Length);
            return packet;
        }

        private byte[] Encrypt(byte[] key, byte[] iv, byte[] plaintext)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        }
    }
}