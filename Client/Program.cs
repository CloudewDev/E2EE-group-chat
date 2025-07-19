using System;

using ServerConnector_ns;
using SessionManager_ns;
using JsonController_ns;
using CommunicationManager_ns;
using ClientRunner_ns;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            string nickname;
            do
            {
                Console.Write("what is your nickname? (more than 1 char)");
                nickname = Console.ReadLine();
            }
            while (string.IsNullOrWhiteSpace(nickname.Trim()));
            ServerConnector_ns.ServerConnector server_connector = new ServerConnector_ns.ServerConnector();
            await server_connector.Init(args[0], args[1]);
            SessionManager_ns.SessionManager session_manager = new SessionManager_ns.SessionManager();
            JsonController_ns.JsonController json_controller = new JsonController_ns.JsonController();
            CommunicationManager_ns.Reciever reciever = new Reciever(server_connector, session_manager, json_controller);
            CommunicationManager_ns.Sender sender = new Sender(server_connector, session_manager, json_controller, nickname);
            ClientRunner_ns.ClientRunner client_runner = new ClientRunner_ns.ClientRunner(server_connector, sender, reciever);
            await client_runner.Run();
        }
        catch (Exception ex) { 
    
            Console.WriteLine(ex.ToString());
        }

    }


}