#include "Server.h"
#include "Listener.h"
#include "ClientManager.h"
#include "IOManager.h"

#include <sys/socket.h>
#include <sys/epoll.h>
#include <unistd.h>
#include <iostream>
#include <arpa/inet.h>
#include <vector>

Server::Server(Listener &listener, ClientManager &client_manager, IOepollManager &io_epoll_manager)
    : listener(listener),
      client_manager(client_manager),
      io_epoll_manager(io_epoll_manager) {}
void Server::run()
{
    const int BUFFER_SIZE = 1024;
    std::vector<char> buffer(BUFFER_SIZE);

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
                int bytes_read = read(events.at(i).data.fd, buffer.data(), BUFFER_SIZE);
                if (bytes_read <= 0)
                {
                    // std::cout << "Client disconnected: " << events.at(i).data.fd << std::endl;
                    client_manager.removeClient(events.at(i).data.fd);
                }
                else
                {
                    std::cout << "message recieved" << std::endl;
                    std::string received_message(buffer.data(), bytes_read);
                    std::string message_to_send = std::to_string(events.at(i).data.fd) + " : " + received_message;
                    std::cout << message_to_send << std::endl;
                    client_manager.broadCastMsg(events.at(i).data.fd, message_to_send);
                }
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
