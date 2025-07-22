#include "Server.h"
#include "Listener.h"
#include "ClientManager.h"
#include "IOManager.h"
#include "Communicator.h"
#include "JsonController.h"
#include "DHCalculator.h"

#include <sys/socket.h>
#include <sys/epoll.h>
#include <unistd.h>
#include <iostream>
#include <arpa/inet.h>
#include <vector>
#include <cstring>
#include <string>

Server::Server(Listener &ls, ClientManager &cm, IOepollManager &iem, Reciever& rv, JsonController& jc, DHCalculator& dc)
    : listener(ls),
      client_manager(cm),
      io_epoll_manager(iem),
      reciever(rv),
      json_controller(jc),
      dh_calculator(dc) {}

void Server::run()
{   
    std::cout << "server is running" << std::endl;

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
                if (input_bytes_str == ""){
                    // std::cout << "user disconnected" << std::endl;
                
                }
                else{
                    int msg_length = 0;
    
                    std::memcpy(&msg_length, input_bytes_str.data(), sizeof(int));
                    std::string recieved_data = reciever.ReadNBytes(msg_length, events.at(i).data.fd);
                    std::cout << "recieved " << recieved_data << std::endl;
                    
                    if (json_controller.parseTypeFromJson(recieved_data) == 0
                        && json_controller.parseToFromJson(recieved_data) == "group")
                    {
                        client_manager.broadCastMsg(MakePacket(msg_length, recieved_data));
                    }
                    else if (json_controller.parseTypeFromJson(recieved_data) == 1
                            && json_controller.parseToFromJson(recieved_data) == "server")
                    {
                        std::string me = "server";
                        std::string opponent = json_controller.parseFromFromJson(recieved_data);
                        client_manager.SetClientNickname(events.at(i).data.fd, opponent);
                        std::cout << "handshake requested from " << opponent << std::endl;
                        std::string my_num = dh_calculator.SetMyNum();
                        
                        std::string data_to_send = json_controller.buildJson(1, me, opponent, my_num);
                        std::string shared_secret = dh_calculator.CalculateSharedSecret(json_controller.parseBodyFromJson(recieved_data));
                        std::cout << "now sending " << data_to_send << std::endl;
                        client_manager.SendMsg(opponent, MakePacket(data_to_send.size(), data_to_send));
                        std::cout << "shared secret is " << shared_secret << std::endl;

                        
                    }
                    else{
                        client_manager.SendMsg(json_controller.parseToFromJson(recieved_data), MakePacket(msg_length, recieved_data));
                    }

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

std::string Server::MakePacket(int size, std::string message_to_sennd){
    int lengthNetworkOrder = htonl(size);
    std::vector<char> packet(4 + size);
    std::memcpy(packet.data(), &lengthNetworkOrder, 4);
    std::memcpy(packet.data() + 4, message_to_sennd.data(), size);
    std::string data_to_send(packet.begin(), packet.end());
    return data_to_send;
}