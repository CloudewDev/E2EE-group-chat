using System;
using System.Net;
using System.Net.Sockets;


namespace ServerConnector_ns
{
	public class ServerConnector
	{

        private static readonly Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		public Socket Sock{
			get { return sock; }
		}
		public async Task Init(string ip, string port)
		{
			IPAddress serverAddr = IPAddress.Parse(ip);
			IPEndPoint clientEP = new IPEndPoint(serverAddr, int.Parse(port));
			await sock.ConnectAsync(clientEP);
			Console.WriteLine("Connection Seccess");
		}

		public async Task CloseSockAsync()
		{
			Console.WriteLine("Closisng Connection");
			await sock.DisconnectAsync(false);
			sock.Dispose();
		}
	}

}