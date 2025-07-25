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


class Server{
public:
    Server(Listener& ls, ClientManager& cm, IOepollManager& iem, Reciever& rc, JsonController& jc, DHCalculator& dc);
    void run();
private:
    Listener& listener;
    ClientManager& client_manager; 
    IOepollManager& io_epoll_manager;
    Reciever& reciever;
    JsonController& json_controller;
    DHCalculator& dh_calculator;

    void setup_client(int listener_fd);
    std::string MakePacket(int size, std::string message_to_sennd);
    std::vector<unsigned char> encrypt(const unsigned char* plaintext, int plaintext_len,
        const unsigned char* key, const unsigned char* iv);
    std::vector<unsigned char> decrypt(const unsigned char* ciphertext, int ciphertext_len,
        const unsigned char* key, const unsigned char* iv);

};

#endif