using ClientRunner_ns;
using Communicator_ns;
using DHShare_ns;
using HandshakeStateMachine_ns;
using JsonController_ns;
using ServerConnector_ns;
using System;
using System.Threading.Channels;

class Program
{
    static async Task<int> Main(string[] args)
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
            Channel<Communicator_ns.Communicator.SEND_TYPE> key_exchange_queue = Channel.CreateBounded<Communicator_ns.Communicator.SEND_TYPE>(1);
            SemaphoreSlim state_semaphore = new SemaphoreSlim(1, 1);
            Communicator_ns.Sender sender = new Sender(server_connector, handshake_state_machine, json_controller, nickname, state_semaphore, key_exchange_queue);
            await sender.Init();
            Communicator_ns.Reciever reciever = new Reciever(server_connector, handshake_state_machine, json_controller, nickname, state_semaphore, key_exchange_queue);
            ClientRunner_ns.ClientRunner client_runner = new ClientRunner_ns.ClientRunner(server_connector, sender, reciever);

            await client_runner.Run();
        }
        catch (Exception ex) { 
    
            Console.WriteLine(ex.ToString());
        }
        return 0;
    }


}