#include "ServerConnector.h"
#include "SharedContents.h"
#include "Sender.h"
#include "Reciever.h"
#include "Client.h"

#include <iostream>
#include <system_error>

int main(int argc, char** argv) {
	ThreadController thread_controller;
	try{
		ServerConnector server_connector(argv[1], argv[2]);
		Sender sender(server_connector, thread_controller);
		Reciever reciever(server_connector, thread_controller);
		Client client(server_connector, sender, reciever, thread_controller);
		
		client.run();
	}
	catch (const std::system_error& e) {
        std::cerr << "initialization failed: " << e.what() << " (Code: " << e.code() << ")\n";
        return -1;
    }
    catch (const std::exception& e) {
        std::cerr << "error: " << e.what() << "\n";
        return -1;
    }
	return 0;
}