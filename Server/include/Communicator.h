#ifndef COMMUNICATOR_H
#define COMMUNICATOR_H

#include "JsonController.h"

#include <string>
#include <vector>

class Communicator
{
public:
    Communicator(JsonController &jc);

protected:
    JsonController &json_controller;
};

class Sender : Communicator
{

public:
    Sender(JsonController &jc);
    std::string MakePacket(int size, std::string message_to_sennd);
    std::vector<unsigned char> encrypt(const unsigned char *plaintext, int plaintext_len,
                                       const unsigned char *key, const unsigned char *iv);

private:
};

class Reciever : Communicator
{
public:
    Reciever(JsonController &jc);
    std::string ReadNBytes(int n, int sock);
    std::vector<unsigned char> decrypt(const unsigned char *ciphertext, int ciphertext_len,
                                       const unsigned char *key, const unsigned char *iv);

private:
};

#endif