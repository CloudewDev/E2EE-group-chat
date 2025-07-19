using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

using ServerConnector_ns;
using SessionManager_ns;
using JsonController_ns;

namespace CommunicationManager_ns
{
    public class CommunicationManager
    {
        protected readonly ServerConnector_ns.ServerConnector server_connector;
        protected readonly SessionManager_ns.SessionManager session_manager;
        protected readonly JsonController_ns.JsonController json_controller;
        protected readonly Socket sock;
        public CommunicationManager(ServerConnector_ns.ServerConnector sc,
            SessionManager_ns.SessionManager sm,
            JsonController_ns.JsonController jp)
        {
            server_connector = sc;
            session_manager = sm;
            sock = server_connector.Sock;
            json_controller = jp;
        }
    }

    public class Reciever : CommunicationManager
    {
        public Reciever(ServerConnector_ns.ServerConnector sc,
            SessionManager_ns.SessionManager sm,
            JsonController_ns.JsonController jp) : base(sc, sm, jp)
        {
        }
        private const int byte_size = 1024;
        private byte[] buffer = new byte[byte_size];
        private string? message;
        private int bytes_read;

        public async Task LoopRecieveAsync(CancellationToken token)
        {

            while (true)
            {

                bytes_read = await sock.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);

                if (bytes_read == 0)
                {
                    await server_connector.CloseSockAsync();
                    Console.WriteLine("server closed");
                    break;
                }
                else
                {
                    message = Encoding.UTF8.GetString(buffer, 0, bytes_read);
                    await Task.Run(() => Console.WriteLine("-> " + message));

                }

            }

        }
    }

    public class Sender : CommunicationManager
    {
        public Sender(ServerConnector_ns.ServerConnector sc,
                SessionManager_ns.SessionManager sm,
                JsonController_ns.JsonController jp,
                string nickname) : base(sc, sm, jp)

        {
            this.nickname = nickname;
        }
        public readonly string nickname;

        private string? line;
        public async Task LoopSendAsync(CancellationToken token)
        {
            while (true)
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
                    line = json_controller.BuildJson(JsonController.MSG_TYPE.Message, nickname, line);
                    byte[] buffer = Encoding.UTF8.GetBytes(line);

                    byte[] lengthPrefix = BitConverter.GetBytes(buffer.Length);
                    Array.Reverse(lengthPrefix);
                    byte[] packet = new byte[lengthPrefix.Length + buffer.Length];
                    Buffer.BlockCopy(lengthPrefix, 0, packet, 0, 4);
                    Buffer.BlockCopy(buffer, 0, packet, 4, buffer.Length);

                    await sock.SendAsync(new ArraySegment<byte>(packet), SocketFlags.None);
                }
            }

        }
    }
}