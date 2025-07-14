#include "Listener.h"
#include "Client.h"
#include "ClientManager.h"
#include "IOManager.h"
#include "Server.h"

#include <iostream>
#include <cstdlib>
#include <system_error>


int main(int argc, char *argv[]){
    try{
        Listener listener(atoi(argv[1]));
        IOepollManager io_epoll_manager(listener.getSockFd());
        ClientManager client_manager(io_epoll_manager);
        Server server(listener, client_manager, io_epoll_manager);

        server.run();
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