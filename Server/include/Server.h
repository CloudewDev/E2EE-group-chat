#ifndef SERVER_H
#define SERVER_H

#include "Listener.h"
#include "ClientManager.h"
#include "IOManager.h"
#include "Communicator.h"
#include "JsonController.h"


class Server{
public:
    Server(Listener& listener, ClientManager& client_manager, IOepollManager& io_epoll_manager, Reciever& rc, JsonController& jc);
    void run();
private:
    Listener& listener;
    ClientManager& client_manager; 
    IOepollManager& io_epoll_manager;
    Reciever& reciever;
    JsonController& json_controller;
    void setup_client(int listener_fd);

};

#endif