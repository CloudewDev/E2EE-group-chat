#ifndef SERVER_H
#define SERVER_H

#include "Listener.h"
#include "ClientManager.h"
#include "IOManager.h"
#include "Communicator.h"
#include "JsonController.h"
#include "DHCalculator.h"

#include <string>
#include <openssl/evp.h>
#include <openssl/rand.h>
#include <cstring>

class Server
{
public:
    Server(Listener &ls, ClientManager &cm, IOepollManager &iem, Sender &sd, Reciever &rc, JsonController &jc, DHCalculator &dc);
    void run();

private:
    Listener &listener;
    ClientManager &client_manager;
    IOepollManager &io_epoll_manager;
    Sender &sender;
    Reciever &reciever;
    JsonController &json_controller;
    DHCalculator &dh_calculator;

    void setup_client(int listener_fd);
    void EncryptAndBroadcast(int type, int change_sock_fd, std::string me);
};

#endif