#ifndef CLIENT_H
#define CLIENT_H

class Client{
public:
    Client(int fd);
    int getSockFd() const;
private:
    int socket_fd;
};

#endif