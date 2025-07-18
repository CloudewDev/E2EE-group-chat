using CommunicationManager_ns;
using ServerConnector_ns;
using System;
using System.Net.Sockets;

namespace ClientRunner_ns
{
    public class ClientRunner
    {
        private readonly ServerConnector_ns.ServerConnector server_connector;
        private readonly CommunicationManager_ns.CommunicationManager communication_manager;
        private readonly Socket sock;

        public ClientRunner(ServerConnector_ns.ServerConnector sc, CommunicationManager_ns.CommunicationManager cm)
        {
            server_connector = sc;
            communication_manager = cm;
            sock = server_connector.Sock;
        }

        public async Task Run()
        {
            await Task.WhenAll(
                communication_manager.LoopRecieveAsync(),
                communication_manager.LoopSendAsync()
            );
        }
    }
}
