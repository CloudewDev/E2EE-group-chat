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
#include <map>
#include <cstring>
#include <string>
#include <memory>
#include <openssl/evp.h>
#include <openssl/rand.h>

Server::Server(Listener &ls, ClientManager &cm, IOepollManager &iem, Sender &sd, Reciever &rv, JsonController &jc, DHCalculator &dc)
    : listener(ls),
      client_manager(cm),
      io_epoll_manager(iem),
      sender(sd),
      reciever(rv),
      json_controller(jc),
      dh_calculator(dc) {}

void Server::run()
{
    std::cout << "server is running" << std::endl;

    while (true)
    {   
        //block here
        int event_counts = io_epoll_manager.watch();

        const std::vector<struct epoll_event> &events = io_epoll_manager.getEvents();
        
        for (int i = 0; i < event_counts; i++)
        {
            if (events.at(i).data.fd == listener.getSockFd())
            {
                // if event is from listing socket, it means there is connection request. handle this.
                setup_client(listener.getSockFd()); 
            }
            else
            {
                std::string me = "server";
                std::string opponent;

                //read the size of current message
                std::string input_bytes_str = reciever.ReadNBytes(4, events.at(i).data.fd);

                if (input_bytes_str == "")
                {
                    std::cout << "[log]announcing group to someone exited" << std::endl;
                    EncryptAndBroadcast(4, events.at(i).data.fd, me);
                    std::cout << "[log]user " << client_manager.SockToName(events.at(i).data.fd) << " disconnected" << std::endl;
                    client_manager.removeClient(events.at(i).data.fd);
                }
                else
                {
                    //type change from byte to int
                    int msg_length = 0;
                    std::memcpy(&msg_length, input_bytes_str.data(), sizeof(int));
                    //read the message
                    std::string recieved_data = reciever.ReadNBytes(msg_length, events.at(i).data.fd);
                    std::cout << "[log]server recieved " << recieved_data << std::endl;

                    if (json_controller.parseToFromJson(recieved_data) == "group")
                    {
                        //this means current message is just talking
                        client_manager.broadCastMsg(events.at(i).data.fd, sender.MakePacket(msg_length, recieved_data));
                    }
                    else if (json_controller.parseToFromJson(recieved_data) == "server")
                    {
                        if (json_controller.parseTypeFromJson(recieved_data) == 1)
                        {
                            //this means DH share handshake request from client
                            std::string opponent = json_controller.parseFromFromJson(recieved_data);
                            
                            //since this process is occured only in first connection, remember the client's nickname now.
                            client_manager.SetClientNickname(events.at(i).data.fd, opponent);

                            std::cout << "[log]handshake requested from " << opponent << std::endl;
                            
                            std::string my_num = dh_calculator.SetMyNum();
                            dh_calculator.CalculateSharedSecret(json_controller.parseBodyFromJson(recieved_data));
                            client_manager.SetClientKey(events.at(i).data.fd, std::move(dh_calculator.GetKey()));

                            std::string temp = ""; //iv is not necessary now. so just input blank
                            std::string data_to_send = json_controller.buildJson(1, me, opponent, my_num, temp);
                            std::cout << "[log]now sending handshake respond" << std::endl;
                            //answer the handshake
                            client_manager.SendMsg(opponent, sender.MakePacket(data_to_send.size(), data_to_send));

                            std::string to = "group";
                            std::cout << "[log]announcing group to someone entered" << std::endl;
                            EncryptAndBroadcast(3, events.at(i).data.fd, me);
                        }
                    }
                    else
                    { // 1:1 handshake
                        client_manager.SendMsg(json_controller.parseToFromJson(recieved_data), sender.MakePacket(msg_length, recieved_data));
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

void Server::EncryptAndBroadcast(int type, int change_sock_fd, std::string me)
{
    //build random iv
    unsigned char iv[16];
    if (RAND_bytes(iv, sizeof(iv)) != 1)
    {
        throw std::runtime_error("RAND_bytes failed");
    }

    //this function is called only when someone exited or entered.
    //so the name of exited/entered user is going to body of message
    std::string body = client_manager.SockToName(change_sock_fd);
    std::vector<unsigned char> message(body.begin(), body.end());
    const std::map<int, std::unique_ptr<Client>> &clients = client_manager.GetSockToClientMap();

    for (const auto &[fd, client_ptr] : clients)
    {
        if (fd == change_sock_fd)
            continue;

        //encrypt the body and broadcast
        std::string opponent = client_ptr->GetName();
        std::vector<unsigned char> key_data = client_manager.GetClientKey(fd);
        std::vector<unsigned char> key(key_data.begin(), key_data.begin() + 32);

        std::vector<unsigned char> encrypted_msg = sender.encrypt(message.data(), message.size(), key.data(), iv);

        std::string body_to_send = dh_calculator.base64Encode(encrypted_msg.data(), encrypted_msg.size());

        std::string iv_to_send = dh_calculator.base64Encode(iv, 16);
        std::string data_to_send = json_controller.buildJson(type, me, opponent, body_to_send, iv_to_send);

        std::cout << "[log]sent" << data_to_send << std::endl;
        client_manager.SendMsg(opponent, sender.MakePacket(data_to_send.size(), data_to_send));
    }
}