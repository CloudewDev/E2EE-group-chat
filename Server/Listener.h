#ifndef LISTENER_H
#define LISTENER_H

class Listener{
public:
    Listener(int port);
    int getSockFd() const;

private:
    int socket_fd;
    
    void setupSocket(int port);

};
#endif