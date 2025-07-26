#ifndef IO_MANAGER_H
#define IO_MANAGER_H

#include <vector>
#include <sys/epoll.h>

class IOepollManager
{
public:
    IOepollManager(int listener_fd);

    void addToEpoll(int sock_fd);
    void RemoveFromEpoll(int sock_fd);

    int watch();

    const std::vector<struct epoll_event> &getEvents() const;

private:
    int epoll_fd;
    int MAX_EVENTS;
    std::vector<struct epoll_event> events;
    int event_counts = 0;
    void setupEpoll(int listener_fd);
};

#endif