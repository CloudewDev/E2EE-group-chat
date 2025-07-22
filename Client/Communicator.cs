using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Numerics;
using System.Security.Cryptography;

using ServerConnector_ns;
using HandshakeStateMachine_ns;
using JsonController_ns;

namespace Communicator_ns
{
    public class Communicator
    {
        protected readonly ServerConnector_ns.ServerConnector server_connector;
        protected readonly HandshakeStateMachine_ns.HandshakeStateMachine handshake_state_machine;
        protected readonly JsonController_ns.JsonController json_controller;
        protected readonly Socket sock;
        public Communicator(ServerConnector_ns.ServerConnector sc,
            HandshakeStateMachine_ns.HandshakeStateMachine hs,
            JsonController_ns.JsonController jp)
        {
            server_connector = sc;
            handshake_state_machine = hs;
            sock = server_connector.Sock;
            json_controller = jp;
        }
    }

    public class Reciever : Communicator
    {
        public Reciever(ServerConnector_ns.ServerConnector sc,
            HandshakeStateMachine_ns.HandshakeStateMachine hs,
            JsonController_ns.JsonController jp) : base(sc, hs, jp)
        {}

        public async Task LoopRecieveAsync(CancellationToken token)
        {

            while (true)
            {
                string message = await RecieveAsync();
                Console.WriteLine("recieved" + message);
                if (handshake_state_machine.MyState == HandshakeStateMachine.STATE.rcv_handshake)
                {
                    if (json_controller.ParseFromFromJson(message) == handshake_state_machine.CurrentHandShaker &&
                        json_controller.ParseTypeFromJson(message) == (int)JsonController.MSG_TYPE.dh)
                    {
                        handshake_state_machine.GetSharedSecret(json_controller.ParseBodyFromJson(message));
                        Console.WriteLine("I recieved opponent's number. I got shared secret. It's " + handshake_state_machine.SharedSecret);
                        handshake_state_machine.SetMyState_ToIdle();
                    }
                    else
                    {
                        Console.WriteLine("recieved message is not from the one who is handshaking with me. ignore this.");
                    }
                }
                else
                {
                    switch (json_controller.ParseTypeFromJson(message))
                    {
                        case (int)JsonController.MSG_TYPE.message:
                            await Task.Run(() => Console.WriteLine(json_controller.ParseFromFromJson(message) + " : " + json_controller.ParseBodyFromJson(message)));
                            break;
                        case (int)JsonController.MSG_TYPE.dh :
                            handshake_state_machine.SetMyState_ToSendHandShake();
                            handshake_state_machine.GetSharedSecret(json_controller.ParseBodyFromJson(message));
                            Console.WriteLine("I got handshake request.");
                            break;
                        case (int)JsonController.MSG_TYPE.sender_key :
                            break;
                        case (int)JsonController.MSG_TYPE.announce : 
                            break;
                    }

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
        public readonly string nickname;
        public Sender(ServerConnector_ns.ServerConnector sc,
            HandshakeStateMachine_ns.HandshakeStateMachine hs,
            JsonController_ns.JsonController jp,
            string nickname) : base(sc, hs, jp)

        {
            this.nickname = nickname;
        }

        private string? line;
        public async Task LoopSendAsync(CancellationToken token)
        {
            while (true)
            {
                if (handshake_state_machine.MyState == HandshakeStateMachine.STATE.send_handshake)
                {
                    line = json_controller.BuildJson(JsonController.MSG_TYPE.dh, nickname, handshake_state_machine.CurrentHandShaker, handshake_state_machine.MyBigNum);
                    await sock.SendAsync(new ArraySegment<byte>(MakeBytesFormat(line)), SocketFlags.None);
                    if (handshake_state_machine.SharedSecret == "0")
                    {
                        handshake_state_machine.SetMyState_ToRcvHandShake();
                        Console.WriteLine("I sent my number and waiting for opponent's number");
                    }
                    else
                    {
                        handshake_state_machine.SetMyState_ToIdle();
                        Console.WriteLine("I'm the side who get handshake. I sended my number.");
                    }
                }
                else
                {
                    line = await Task.Run(() => Console.ReadLine());
                    if (string.IsNullOrEmpty(line)) continue;
                    if (line == "/quit")
                    {
                        await server_connector.CloseSockAsync();
                        break;
                    }
                    else
                    {
                        line = json_controller.BuildJson(JsonController.MSG_TYPE.message, nickname, "group", line);
                        await sock.SendAsync(new ArraySegment<byte>(MakeBytesFormat(line)), SocketFlags.None);
                    }

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