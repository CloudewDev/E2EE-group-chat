using System;
using System.Net.Sockets;
using System.Threading.Tasks;

using Communicator_ns;
using ServerConnector_ns;
namespace ClientRunner_ns
{
    public class ClientRunner
    {
        private readonly ServerConnector_ns.ServerConnector server_connector;
        private readonly Communicator_ns.Sender sender;
        private readonly Communicator_ns.Reciever reciever;
        private readonly Socket sock;

        public ClientRunner(ServerConnector_ns.ServerConnector sc, Communicator_ns.Sender sd, Communicator_ns.Reciever rc)
        {
            server_connector = sc;
            sender = sd;
            reciever = rc;
            sock = server_connector.Sock;
        }

        public async Task Run()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            //Console.WriteLine(sender == null ? "sender is null" : "sender is OK");
            Task recieve = reciever.LoopRecieveAsync(token);
            Console.WriteLine("Loop Recieve Async task built");
            Task send_input = sender.LoopInputAsync(token);
            Console.WriteLine("Loop Input Async task built");
            Task send_system = sender.LoopSendAsync(token);
            Console.WriteLine("Loop Send Async task built");

            await Task.WhenAny(recieve, send_system, send_input);
            cts.Cancel();

            await Task.WhenAll(recieve, send_system, send_input);
        }
    }
}
