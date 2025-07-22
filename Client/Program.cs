using System;

using ServerConnector_ns;
using HandshakeStateMachine_ns;
using JsonController_ns;
using Communicator_ns;
using ClientRunner_ns;
using DHShare_ns;

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

            DHShare_ns.DHShare dh_share = new DHShare_ns.DHShare();
            HandshakeStateMachine_ns.HandshakeStateMachine handshake_state_machine = new HandshakeStateMachine_ns.HandshakeStateMachine(dh_share);
            ServerConnector_ns.ServerConnector server_connector = new ServerConnector_ns.ServerConnector(handshake_state_machine);
            await server_connector.Init(args[0], args[1]);

            JsonController_ns.JsonController json_controller = new JsonController_ns.JsonController();
            Communicator_ns.Reciever reciever = new Reciever(server_connector, handshake_state_machine, json_controller);
            Communicator_ns.Sender sender = new Sender(server_connector, handshake_state_machine, json_controller, nickname);
            ClientRunner_ns.ClientRunner client_runner = new ClientRunner_ns.ClientRunner(server_connector, sender, reciever);

            await client_runner.Run();
        }
        catch (Exception ex) { 
    
            Console.WriteLine(ex.ToString());
        }

    }


}