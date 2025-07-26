#include "IOManager.h"

#include <unistd.h>
#include <sys/epoll.h>
#include <system_error>

IOepollManager::IOepollManager(int listener_fd) : MAX_EVENTS(1024), events(MAX_EVENTS)
{
    setupEpoll(listener_fd);
}

void IOepollManager::addToEpoll(int sock_fd)
{
    struct epoll_event client_event;
    client_event.events = EPOLLIN;
    client_event.data.fd = sock_fd;
    if (epoll_ctl(epoll_fd, EPOLL_CTL_ADD, sock_fd, &client_event) < 0)
    {
        throw std::system_error(errno, std::generic_category(), "epoll_ctl() failed");
    }
}

void IOepollManager::RemoveFromEpoll(int sock_fd)
{
    epoll_ctl(epoll_fd, EPOLL_CTL_DEL, sock_fd, nullptr);
}

int IOepollManager::watch()
{
    event_counts = epoll_wait(epoll_fd, events.data(), MAX_EVENTS, -1);
    if (event_counts < 0)
    {
        throw std::system_error(errno, std::generic_category(), "epoll_wait() failed");
    }
    return event_counts;
}

const std::vector<struct epoll_event> &IOepollManager::getEvents() const
{
    return events;
}

void IOepollManager::setupEpoll(int listener_fd)
{
    struct epoll_event event_type;
    epoll_fd = epoll_create(1);
    if (epoll_fd < 0)
    {
        throw std::system_error(errno, std::generic_category(), "epoll_create() failed");
    }
    event_type.events = EPOLLIN;
    event_type.data.fd = listener_fd;
    if (epoll_ctl(epoll_fd, EPOLL_CTL_ADD, listener_fd, &event_type) < 0)
    {
        close(epoll_fd);
        close(listener_fd);
        throw std::system_error(errno, std::generic_category(), "epoll_ctl() failed");
    }
}