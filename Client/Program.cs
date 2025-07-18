using System;

using CommunicationManager_ns;
using ServerConnector_ns;
using ClientRunner_ns;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            ServerConnector_ns.ServerConnector server_connector = new ServerConnector_ns.ServerConnector();
            await server_connector.Init(args[0], args[1]);
            CommunicationManager_ns.CommunicationManager communication_manager = new CommunicationManager_ns.CommunicationManager(server_connector);
            ClientRunner_ns.ClientRunner client_runner = new ClientRunner_ns.ClientRunner(server_connector, communication_manager);
            await client_runner.Run();
        }
        catch (Exception ex) { 
    
            Console.WriteLine(ex.ToString());
        }

    }


}