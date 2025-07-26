#include "Client.h"

#include <vector>
#include <string>

Client::Client(int fd)
{
    socket_fd = fd; // set the socket by constructor
}

int Client::getSockFd() const
{
    return socket_fd;
}

std::string Client::GetName()
{
    return nickname;
}
void Client::SetName(std::string input)
{
    nickname = input;
}

const std::vector<unsigned char> &Client::GetKey() const
{
    return *my_key;
}

void Client::SetKey(std::vector<unsigned char> &&input)
{
    //since key is freaquently used, I avoided to copy the whole key every time.
    //and also avoided freeing the memory by hand
    my_key = std::make_unique<std::vector<unsigned char>>(std::move(input));
}