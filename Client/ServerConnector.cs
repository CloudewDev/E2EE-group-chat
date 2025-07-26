using System;
using System.Net;
using System.Net.Sockets;

using HandshakeStateMachine_ns;


namespace ServerConnector_ns
{
    public class ServerConnector
    {

        private static readonly Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public Socket Sock
        {
            get { return sock; }
        }
        private HandshakeStateMachine_ns.HandshakeStateMachine handshake_state_machine;
        public ServerConnector(HandshakeStateMachine_ns.HandshakeStateMachine hm)
        {
            handshake_state_machine = hm;
        }
        public async Task Init(string ip, string port)
        {
            IPAddress serverAddr = IPAddress.Parse(ip);
            IPEndPoint clientEP = new IPEndPoint(serverAddr, int.Parse(port));
            await sock.ConnectAsync(clientEP);
            Console.WriteLine("[log]Connection Seccess");
            handshake_state_machine.SetMyState_ToSendDH();
        }

        public void CloseSockAsync()
        {
            Console.WriteLine("[log]Closisng Connection");
            sock.Shutdown(SocketShutdown.Both);
            sock.Dispose();
        }
    }

}