#ifndef CLIENT_H
#define CLIENT_H

#include <vector>
#include <string>
#include <memory>

class Client{
public:
    Client(int fd);
    int getSockFd() const;

    std::string GetName();
    void SetName(std::string input);
    const std::vector<unsigned char>& GetKey() const;
    void SetKey(std::vector<unsigned char>&& input);
private:
    std::string nickname;
    std::unique_ptr<std::vector<unsigned char>> my_key;
    int socket_fd;
};

#endif