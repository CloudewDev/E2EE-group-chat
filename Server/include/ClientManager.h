#ifndef CLIENT_MANAGER_H
#define CLIENT_MANAGER_H

#include "IOManager.h"
#include "Client.h"

#include <map>
#include <memory>


class ClientManager{
public:
    ClientManager(IOepollManager& io_epoll_manager);
    void addClient(int socket_fd);
    void removeClient(int client_socket_fd);
    void broadCastMsg(std::string message);

private:
    IOepollManager& io_epoll_manager;
    std::map<int, std::unique_ptr<Client>> client_map;
};

#endif