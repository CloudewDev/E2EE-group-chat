#ifndef SERVER_H
#define SERVER_H

#include "Listener.h"
#include "ClientManager.h"
#include "IOManager.h"


class Server{
public:
    Server(Listener& listener, ClientManager& client_manager, IOepollManager& io_epoll_manager);
    void run();
private:
    Listener& listener;
    ClientManager& client_manager; 
    IOepollManager& io_epoll_manager;
    void setup_client(int listener_fd);
};

#endif