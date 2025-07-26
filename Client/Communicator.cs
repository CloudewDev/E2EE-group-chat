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
        protected readonly HandshakeStateMachine_ns.HandshakeStateMachine handshake_state_machine;
        protected readonly JsonController_ns.JsonController json_controller;
        protected readonly Ratchet_ns.Ratchet ratchet;
        protected readonly Socket sock;
        protected readonly string nickname;
        protected readonly SemaphoreSlim state_semaphore;
        protected readonly SemaphoreSlim ratchet_semaphore;
        public enum SEND_TYPE { rcv_first, send_first }
        protected Channel<SEND_TYPE> dh_exchange_queue;
        protected Channel<SEND_TYPE> key_exchange_queue;
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
                        await Task.Run(() =>
                        Console.WriteLine(
                                    json_controller.ParseFromFromJson(message) 
                                    + " : " 
                                    + Encoding.UTF8.GetString(Decrypt(ratchet.AccessData(json_controller.ParseFromFromJson(message)).sender_key,
                                        Convert.FromBase64String(json_controller.ParseIVFromJson(message)),
                                        Convert.FromBase64String(json_controller.ParseBodyFromJson(message)))
                                     )
                                )
                        );
                        Console.ResetColor();
                        break;
                    case (int)JsonController.MSG_TYPE.dh:
                        if (handshake_state_machine.MyState == HandshakeStateMachine.STATE.waiting_for_rcv_dh)
                        {
                            if (json_controller.ParseFromFromJson(message) == handshake_state_machine.CurrentHandShaker)
                            {
                                //Console.WriteLine("recieved answer of key exchange");
                                //byte[] shared_secret_bytes = handshake_state_machine.SharedSecretAsByte;
                                //Console.WriteLine("I recieved opponent's number. shared secret is " + Convert.ToBase64String(shared_secret_bytes));
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
                            //Console.WriteLine("recieved request for me to handshake with opponent");
                            handshake_state_machine.OpponentBigNum = json_controller.ParseBodyFromJson(message);
                            await state_semaphore.WaitAsync(token);
                            try
                            {
                                handshake_state_machine.CurrentHandShaker = json_controller.ParseFromFromJson(message);
                                handshake_state_machine.SetMyState_ToSendDH();
                            }
                            finally { state_semaphore.Release(); }
                            await dh_exchange_queue.Writer.WriteAsync(SEND_TYPE.rcv_first);
                        }
                        else
                        {
                            Console.WriteLine("[log]ignore current message");
                        }
                            break;
                    case (int)JsonController.MSG_TYPE.sender_key:

                        await ratchet_semaphore.WaitAsync(token);
                        try
                        {
                            ratchet.CurrentExchanger = json_controller.ParseFromFromJson(message);
                            ratchet.SetOpponentSenderKey
                                (
                                Convert.FromBase64String(json_controller.ParseIVFromJson(message)),
                                Convert.FromBase64String(json_controller.ParseBodyFromJson(message))
                                );
                            if (ratchet.MyState == Ratchet.STATE.waiting_for_rcv_key)
                            {
                                ratchet.SetMyState_ToRcvedKey();
                            }
                            else if (ratchet.MyState == Ratchet.STATE.idle)
                            {
                                ratchet.SetMyState_ToSendKey();
                            }

                        }
                        finally {  ratchet_semaphore.Release(); }
                        await key_exchange_queue.Writer.WriteAsync(SEND_TYPE.rcv_first);
                        break;
                    case (int)JsonController.MSG_TYPE.enter:
                        if (handshake_state_machine.MyState == HandshakeStateMachine.STATE.idle)
                        {
                            byte[] enter_key = new byte[32];
                            Array.Copy(ratchet.AccessData("server").session_key, 0, enter_key, 0, 32);
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
            byte[] message_byte = new byte[msg_size];
            await ReadNBytesAsync(msg_size, message_byte, token); // ReadNBytesAsync function already has exception handling
            message = Encoding.UTF8.GetString(message_byte, 0, msg_size);
            return message;
        }
        private async Task ReadNBytesAsync(int n, byte[] buffer, CancellationToken token)
        {
            int currentRead = 0;
            int bytes_read = 0; ;
            while (currentRead < n)
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
            Channel<string> update_senderkey_queue = Channel.CreateUnbounded<string>();
            Ratchet.KEYS temp = new Ratchet.KEYS();
            temp.sender_key = ratchet.MySenderKey;
            ratchet.AddData(nickname, temp);
        }

        public async Task Init()
        {
            await dh_exchange_queue.Writer.WriteAsync(SEND_TYPE.send_first);
        }
        private readonly SemaphoreSlim sock_semaphore = new SemaphoreSlim(1, 1);
        public async Task LoopInputAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                string line = await Task.Run(() => Console.ReadLine());
                if (string.IsNullOrEmpty(line)) continue;
                if (line == "/quit")
                {
                    break;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(nickname + " : " + line);
                    Console.ResetColor();
                    using (RandomNumberGenerator r = RandomNumberGenerator.Create()){
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
                //Console.WriteLine("ready to read something");
                SEND_TYPE send_type = await dh_exchange_queue.Reader.ReadAsync(token);
                //Console.WriteLine("key exchange queue got something");

                if (handshake_state_machine.MyState == HandshakeStateMachine.STATE.send_dh)
                {
                    string message;
                    if (send_type == SEND_TYPE.send_first)
                    {
                        message = json_controller.BuildJson
                            (JsonController.MSG_TYPE.dh, nickname, handshake_state_machine.CurrentHandShaker, handshake_state_machine.MyBigNum, "");
                        await state_semaphore.WaitAsync(token);
                        try
                        {
                            handshake_state_machine.SetMyState_ToWaitingForRcvDH();
                        }
                        finally { state_semaphore.Release(); }
                        while (handshake_state_machine.MyState != HandshakeStateMachine.STATE.rcved_dh)
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
                    else
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

        public async Task LoopSenderAsync(CancellationToken token)
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