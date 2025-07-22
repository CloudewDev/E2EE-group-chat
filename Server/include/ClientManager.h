#ifndef CLIENT_MANAGER_H
#define CLIENT_MANAGER_H

#include "IOManager.h"
#include "Client.h"

#include <map>
#include <memory>
#include <string>


class ClientManager{
public:
    ClientManager(IOepollManager& io_epoll_manager);
    void addClient(int socket_fd);
    void removeClient(int client_socket_fd);
    void SetClientNickname(int client_sock_fd, std::string input_name);
    void broadCastMsg(std::string message);
    void SendMsg(std::string to, std::string message);

private:
    IOepollManager& io_epoll_manager;
    std::map<int, std::unique_ptr<Client>> client_map;
    std::map<std::string, int> name2sock_map;
};

#endif