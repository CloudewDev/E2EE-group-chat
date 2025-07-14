#include "Listener.h"

#include <sys/socket.h>
#include <cstring>
#include <string>
#include <arpa/inet.h>
#include <system_error>
#include <iostream>
#include <unistd.h>

Listener::Listener(int port){
    setupSocket(port);
}
int Listener::getSockFd() const{
    return socket_fd;
}
    
void Listener::setupSocket(int port){
    socklen_t optlen;
    struct sockaddr_in serv_addr;
    int option;

    socket_fd = socket(PF_INET, SOCK_STREAM, 0);
    if (socket_fd == -1){
        throw std::system_error(errno, std::generic_category(), "socket() failed");
    }

    optlen = sizeof(option);
    option = 1;
    setsockopt(socket_fd, SOL_SOCKET, SO_REUSEADDR, (void*)&option, optlen);
    memset(&serv_addr, 0, sizeof(serv_addr));

    serv_addr.sin_family = AF_INET;
    serv_addr.sin_addr.s_addr = htonl(INADDR_ANY);
    serv_addr.sin_port = htons(port);

    if (::bind(socket_fd, (struct sockaddr*)&serv_addr, sizeof(serv_addr)) == -1) {
        ::close(socket_fd);
        throw std::system_error(errno, std::generic_category(), "bind() failed");
    }
        
    if (::listen(socket_fd, 10) == -1) {
        ::close(socket_fd);
        throw std::system_error(errno, std::generic_category(), "listen() failed");
    }

    std::cout << "server is waiting on port " << port << std::endl;
}