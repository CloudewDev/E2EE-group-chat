#include "Server.h"
#include "Listener.h"
#include "ClientManager.h"
#include "IOManager.h"
#include "Communicator.h"
#include "JsonController.h"

#include <sys/socket.h>
#include <sys/epoll.h>
#include <unistd.h>
#include <iostream>
#include <arpa/inet.h>
#include <vector>
#include <cstring>

Server::Server(Listener &listener, ClientManager &client_manager, IOepollManager &io_epoll_manager, Reciever& rv, JsonController& jc)
    : listener(listener),
      client_manager(client_manager),
      io_epoll_manager(io_epoll_manager),
      reciever(rv),
      json_controller(jc) {}

void Server::run()
{   

    while (true)
    {

        int event_counts = io_epoll_manager.watch();
        const std::vector<struct epoll_event> &events = io_epoll_manager.getEvents();

        for (int i = 0; i < event_counts; i++)
        {
            if (events.at(i).data.fd == listener.getSockFd())
            {
                setup_client(listener.getSockFd());
            }
            else
            {
                std::string input_bytes_str = reciever.ReadNBytes(4, events.at(i).data.fd);
                int msg_length = 0;

                std::memcpy(&msg_length, input_bytes_str.data(), sizeof(int));
                std::string recieved_msg = reciever.ReadNBytes(msg_length, events.at(i).data.fd);
                
                client_manager.broadCastMsg(events.at(i).data.fd, json_controller.parseBodyFromJson(recieved_msg));
                std::cout << json_controller.parseWhoFromJson(recieved_msg) + " : " + json_controller.parseBodyFromJson(recieved_msg) << std::endl;
            }
        }
    }
}

void Server::setup_client(int listener_fd)
{
    int socket_fd;
    socklen_t clnt_addr_size;
    struct sockaddr_in clnt_addr;

    clnt_addr_size = sizeof(clnt_addr);
    socket_fd = accept(listener_fd, (struct sockaddr *)&clnt_addr, &clnt_addr_size);
    if (socket_fd == -1)
    {
        throw std::system_error(errno, std::generic_category(), "accept() failed");
    }
    else
    {
        client_manager.addClient(socket_fd);
        std::cout << "connection established" << std::endl;
    }
}
