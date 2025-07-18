using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

using ServerConnector_ns;

namespace CommunicationManager_ns
{
    public class CommunicationManager
    {
        private readonly ServerConnector_ns.ServerConnector server_connector;
        private readonly Socket sock;
        public CommunicationManager(ServerConnector_ns.ServerConnector s)
        {
            server_connector = s;
            sock = server_connector.Sock;
        }
        private bool program_done = false;


        private const int byte_size = 1024;
        private byte[] buffer = new byte[byte_size];
        private string message;
        private int bytes_read;
        public async Task LoopRecieveAsync(){

            while (!program_done){

                bytes_read = await sock.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);

                if (bytes_read == 0) {
                    program_done = true;
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

        private void RcvCallback(IAsyncResult ar)
        {
            byte[] buffer = (byte[])ar.AsyncState;
            int bytesRead = sock.EndReceive(ar);
            if (bytesRead > 0)
            {
                message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            }
        }

        private string? line;
        public async Task LoopSendAsync(){
            while (!program_done)
            {
                line = await Task.Run(() => Console.ReadLine());
                if (string.IsNullOrEmpty(line)) continue;

                byte[] buffer = Encoding.UTF8.GetBytes(line);
                if (line == "/quit")
                {
                    program_done = true;
                    await server_connector.CloseSockAsync();
                    break;
                }
                else
                {
                    await Task.Run(() => Console.WriteLine("me : " + line));
                    sock.BeginSend(buffer, 0, buffer.Length, 0, null, null);
                }
            }

        }

    }

}
