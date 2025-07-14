#include "ClientManager.h"
#include "IOManager.h"
#include "Client.h"

#include <map>
#include <memory>
#include <unistd.h>
#include <string>

ClientManager::ClientManager(IOepollManager& io_epoll_manager) : 
    io_epoll_manager(io_epoll_manager) {}

void ClientManager::addClient(int socket_fd){
    std::unique_ptr<Client> client_ptr = std::make_unique<Client>(socket_fd);
    client_map[client_ptr->getSockFd()] = std::move(client_ptr);
    io_epoll_manager.addToEpoll(socket_fd);
}

void ClientManager::removeClient(int client_socket_fd){
    client_map.erase(client_socket_fd);
}

void ClientManager::broadCastMsg(int sender_fd, std::string message){
    for (auto iter = client_map.begin(); iter != client_map.end(); iter++)
    {
        if (iter->first != sender_fd)
        {
            write(iter->first, message.c_str(), message.length());
        }
    }
}
