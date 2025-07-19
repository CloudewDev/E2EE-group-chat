using System;
using System.Net.Sockets;
using System.Threading.Tasks;

using CommunicationManager_ns;
using ServerConnector_ns;
namespace ClientRunner_ns
{
    public class ClientRunner
    {
        private readonly ServerConnector_ns.ServerConnector server_connector;
        private readonly CommunicationManager_ns.Sender sender;
        private readonly CommunicationManager_ns.Reciever reciever;
        private readonly Socket sock;

        public ClientRunner(ServerConnector_ns.ServerConnector sc, CommunicationManager_ns.Sender sd, CommunicationManager_ns.Reciever rc)
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

            Task recieve = reciever.LoopRecieveAsync(token);
            Task send = sender.LoopSendAsync(token);

            await Task.WhenAny(recieve, send);
            cts.Cancel();

            await Task.WhenAll(recieve, send);
        }
    }
}
