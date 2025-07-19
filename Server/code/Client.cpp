#include "Client.h"


Client::Client(int fd){
    socket_fd = fd;
}

int Client::getSockFd() const{
    return socket_fd;
}