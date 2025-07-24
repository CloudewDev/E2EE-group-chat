#include "ClientManager.h"
#include "IOManager.h"
#include "Client.h"

#include <map>
#include <memory>
#include <unistd.h>
#include <string>
#include <iostream>
#include <vector>
#include <memory>

ClientManager::ClientManager(IOepollManager& io_epoll_manager) : 
    io_epoll_manager(io_epoll_manager) {}

void ClientManager::addClient(int socket_fd){
    std::unique_ptr<Client> client_ptr = std::make_unique<Client>(socket_fd);
    client_map[client_ptr->getSockFd()] = std::move(client_ptr);
    io_epoll_manager.addToEpoll(socket_fd);
}

void ClientManager::removeClient(int client_socket_fd){
    client_map.erase(client_socket_fd);
    for (auto iter = name2sock_map.begin() ; iter != name2sock_map.end() ; iter++){
        if (iter -> second == client_socket_fd){
            name2sock_map.erase(iter);
            break;
        }
    }
}

void ClientManager::SetClientNickname(int client_socket_fd, std::string input_name){
    client_map[client_socket_fd]->GetName() = input_name;
    name2sock_map[input_name] = client_socket_fd;
}
void ClientManager::SetClientKey(int client_sock_fd, std::vector<unsigned char>&& input){
    client_map[client_sock_fd] -> SetKey(std::move(input));
}
const std::vector<unsigned char>& ClientManager::GetClientKey(int client_sock_fd) const{
    return client_map.at(client_sock_fd) -> GetKey();
}

void ClientManager::broadCastMsg(std::string message){
    for (auto iter = client_map.begin(); iter != client_map.end(); iter++)
    {
        write(iter->first, message.c_str(), message.length());
    }
}

void ClientManager::SendMsg(std::string to, std::string message){
    write(name2sock_map[to], message.c_str(), message.length());
}