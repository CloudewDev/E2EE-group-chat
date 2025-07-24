using HandshakeStateMachine_ns;
using JsonController_ns;
using ServerConnector_ns;

using System;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;

namespace Communicator_ns
{
    public class Communicator
    {
        protected readonly ServerConnector_ns.ServerConnector server_connector;
        protected readonly HandshakeStateMachine_ns.HandshakeStateMachine handshake_state_machine;
        protected readonly JsonController_ns.JsonController json_controller;
        protected readonly Socket sock;
        protected readonly string nickname;
        protected readonly SemaphoreSlim state_semaphore;
        public enum SEND_TYPE { rcv_first, send_frist }
        protected Channel<SEND_TYPE> key_exchange_queue;
        public Communicator(ServerConnector_ns.ServerConnector sc,
            HandshakeStateMachine_ns.HandshakeStateMachine hs,
            JsonController_ns.JsonController jp,
            string nickname,
            SemaphoreSlim ss,
            Channel<SEND_TYPE> keq)
        {
            server_connector = sc;
            handshake_state_machine = hs;
            sock = server_connector.Sock;
            json_controller = jp;
            this.nickname = nickname;
            state_semaphore = ss;
            key_exchange_queue = keq;
        }
    }

    public class Reciever : Communicator
    {
        public Reciever(ServerConnector_ns.ServerConnector sc,
            HandshakeStateMachine_ns.HandshakeStateMachine hs,
            JsonController_ns.JsonController jp,
            string nickname,
            SemaphoreSlim ss,
            Channel<SEND_TYPE> keq) : base(sc, hs, jp, nickname, ss, keq)
        { }

        public async Task LoopRecieveAsync(CancellationToken token)
        {

            while (!token.IsCancellationRequested)
            {
                string message = await RecieveAsync();
                Console.WriteLine("recieved" + message);
                switch (json_controller.ParseTypeFromJson(message))
                {
                    case (int)JsonController.MSG_TYPE.message:
                        await Task.Run(() => Console.WriteLine(json_controller.ParseFromFromJson(message) + " : " + json_controller.ParseBodyFromJson(message)));
                        break;
                    case (int)JsonController.MSG_TYPE.dh:
                        if (handshake_state_machine.MyState == HandshakeStateMachine.STATE.rcv_handshake)
                        {
                            if (json_controller.ParseFromFromJson(message) == handshake_state_machine.CurrentHandShaker)
                            {
                                Console.WriteLine("recieved answer of key exchange");
                                //byte[] shared_secret_bytes = handshake_state_machine.SharedSecretAsByte;
                                //Console.WriteLine("I recieved opponent's number. shared secret is " + Convert.ToBase64String(shared_secret_bytes));
                                handshake_state_machine.OpponentBigNum = json_controller.ParseBodyFromJson(message);
                                await state_semaphore.WaitAsync();
                                try
                                {
                                    handshake_state_machine.SetMyState_ToIdle();
                                }
                                finally { state_semaphore.Release(); }
                            }
                            else
                            {
                                Console.WriteLine("recieved message is not from the one who is handshaking with me. ignore this.");
                            }
                        }
                        else if (handshake_state_machine.MyState == HandshakeStateMachine.STATE.idle)
                        {
                            Console.WriteLine("recieved request for me to handshake with opponent");
                            handshake_state_machine.OpponentBigNum = json_controller.ParseBodyFromJson(message);
                            await state_semaphore.WaitAsync();
                            try
                            {
                                handshake_state_machine.CurrentHandShaker = json_controller.ParseFromFromJson(message);
                                handshake_state_machine.SetMyState_ToSendHandShake();
                            }
                            finally { state_semaphore.Release(); }
                            Console.WriteLine("since request recieved, trying queue added");
                            await key_exchange_queue.Writer.WriteAsync(SEND_TYPE.rcv_first);
                            Console.WriteLine("queue add succeed");
                        }
                        break;
                    case (int)JsonController.MSG_TYPE.sender_key:
                        await state_semaphore.WaitAsync();
                        try
                        {
                            HandshakeStateMachine.KEYS temp = handshake_state_machine.AccessData(json_controller.ParseFromFromJson(message));
                            temp.sender_key = Encoding.UTF8.GetBytes(json_controller.ParseBodyFromJson(message));
                            handshake_state_machine.ChangeData(json_controller.ParseFromFromJson(message), temp);
                        }
                        finally { state_semaphore.Release(); }
                        break;
                    case (int)JsonController.MSG_TYPE.announce:
                        if (json_controller.ParseBodyFromJson(message) != nickname && handshake_state_machine.MyState == HandshakeStateMachine.STATE.idle)
                        {
                            await state_semaphore.WaitAsync();
                            try
                            {
                                Console.WriteLine("I should talk to " + json_controller.ParseBodyFromJson(message));
                                handshake_state_machine.CurrentHandShaker = json_controller.ParseBodyFromJson(message);
                                Console.WriteLine("current handshaker is " + handshake_state_machine.CurrentHandShaker);
                                handshake_state_machine.SetMyState_ToSendHandShake();
                            }
                            finally { state_semaphore.Release(); }
                            Console.WriteLine("announcement. waiting for queue added");
                            await key_exchange_queue.Writer.WriteAsync(SEND_TYPE.send_frist);
                            Console.WriteLine("announcement. queue add succeed");
                        }
                        break;
                }

            }

        }
        private async Task<string> RecieveAsync()
        {
            byte[] msg_size_data = new byte[4];
            string? message;
            await ReadNBytesAsync(4, msg_size_data);
            Array.Reverse(msg_size_data);
            int msg_size = BitConverter.ToInt32(msg_size_data, 0);
            byte[] message_byte = new byte[msg_size];
            await ReadNBytesAsync(msg_size, message_byte); // ReadNBytesAsync function already has exception handling
            message = Encoding.UTF8.GetString(message_byte, 0, msg_size);
            return message;
        }
        private async Task ReadNBytesAsync(int n, byte[] buffer)
        {
            int currentRead = 0;
            int bytes_read = 0; ;
            while (currentRead < n)
            {
                bytes_read = await sock.ReceiveAsync(new ArraySegment<byte>(buffer, currentRead, n - currentRead), SocketFlags.None);
                if (bytes_read <= 0)
                {
                    await server_connector.CloseSockAsync();
                    throw new Exception("server closed");
                }
                currentRead += bytes_read;

            }

        }
    }

    public class Sender : Communicator
    {
        public Sender(ServerConnector_ns.ServerConnector sc,
            HandshakeStateMachine_ns.HandshakeStateMachine hs,
            JsonController_ns.JsonController jp,
            string nickname,
            SemaphoreSlim ss,
            Channel<SEND_TYPE> keq) : base(sc, hs, jp, nickname, ss, keq) 
        {
            Console.WriteLine("sender constructor. queue added");
        }
        public async Task Init()
        {
            await key_exchange_queue.Writer.WriteAsync(SEND_TYPE.send_frist);
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
                    await server_connector.CloseSockAsync();
                    break;
                }
                else
                {
                    line = json_controller.BuildJson(JsonController.MSG_TYPE.message, nickname, "group", line);
                    await sock_semaphore.WaitAsync();
                    try
                    {
                        await sock.SendAsync(new ArraySegment<byte>(MakeBytesFormat(line)), SocketFlags.None);
                    }
                    finally { sock_semaphore.Release(); }
                }
            }

        }
        public async Task LoopSendAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Console.WriteLine("ready to read something");
                SEND_TYPE send_type = await key_exchange_queue.Reader.ReadAsync(token);
                Console.WriteLine("key exchange queue got something");

                if (handshake_state_machine.MyState == HandshakeStateMachine.STATE.send_handshake)
                {
                    string message;
                    //Console.WriteLine("sended" + line);
                    if (send_type == SEND_TYPE.send_frist)
                    {
                        message = json_controller.BuildJson
                            (JsonController.MSG_TYPE.dh, nickname, handshake_state_machine.CurrentHandShaker, handshake_state_machine.MyBigNum);
                        await state_semaphore.WaitAsync();
                        try
                        {
                            handshake_state_machine.SetMyState_ToRcvHandShake();
                        }
                        finally { state_semaphore.Release(); }
                        Console.WriteLine("waiting for opponent's number");
                    }
                    else
                    {
                        message = json_controller.BuildJson
                           (JsonController.MSG_TYPE.dh, nickname, handshake_state_machine.CurrentHandShaker, handshake_state_machine.MyBigNum);
                        await state_semaphore.WaitAsync();
                        try
                        {
                            handshake_state_machine.SetMyState_ToIdle();
                        }
                        finally { state_semaphore.Release(); }
                        Console.WriteLine("I'm the side who get handshake. I sended my number.");
                    }
                    await sock_semaphore.WaitAsync();
                    try
                    {
                        await sock.SendAsync(new ArraySegment<byte>(MakeBytesFormat(message)), SocketFlags.None);
                        Console.WriteLine("sended key exchange message");
                    }
                    finally { sock_semaphore.Release(); }
                }
                else
                {
                    Console.WriteLine("something is wrong. state is not send_handshake but got key_exchange_queue");
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
    }
}