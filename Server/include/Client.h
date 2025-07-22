#ifndef CLIENT_H
#define CLIENT_H

#include <vector>
#include <string>

class Client{
public:
    Client(int fd);
    int getSockFd() const;

    std::string nickname;
    std::vector<char> key;
private:
    int socket_fd;
};

#endif