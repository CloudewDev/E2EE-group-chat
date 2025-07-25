#include "Listener.h"
#include "Client.h"
#include "ClientManager.h"
#include "IOManager.h"
#include "Communicator.h"
#include "JsonController.h"
#include "DHCalculator.h"
#include "Server.h"

#include <iostream>
#include <cstdlib>
#include <system_error>


int main(int argc, char *argv[]){
    try{
        Listener listener(atoi(argv[1]));
        std::cout << "[log]listener on" << std::endl;
        IOepollManager io_epoll_manager(listener.getSockFd());
        std::cout << "[log]epoll on" << std::endl;
        ClientManager client_manager(io_epoll_manager);
        std::cout << "[log]client manager on" << std::endl;
        JsonController jscon_controller;
        std::cout << "[log]json controller on" << std::endl;
        Reciever reciever(jscon_controller);
        std::cout << "[log]reciever on" << std::endl;
        DHCalculator dh_calculator;
        std::cout << "[log]DH calculator on" << std::endl;
        Server server(listener, client_manager, io_epoll_manager, reciever, jscon_controller, dh_calculator);

        server.run();
    }
    catch (const std::system_error& e) {
        std::cerr << "[log]initialization failed: " << e.what() << " (Code: " << e.code() << ")\n";
        return -1;
    }
    catch (const std::exception& e) {
        std::cerr << "error: " << e.what() << "\n";
        return -1;
    }

	return 0;

}