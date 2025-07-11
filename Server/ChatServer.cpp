#include <iostream>
#include <sys/epoll.h>
#include <sys/socket.h>
#include <arpa/inet.h>
#include <unistd.h>
#include <cstring>
#include <system_error>
#include <vector>
#include <list>
#include <memory>
#include <map>

class Listener;
class Client;
class ClientManager;
class IOepollManager;
class Server;


class Listener{
public:
    explicit Listener(int port){
        setupSocket(port);
    }
    int getSockFd() const{
        return socket_fd;
    }

private:
    int socket_fd;
    
    void setupSocket(int port){
        socklen_t optlen;
        struct sockaddr_in serv_addr;
        int option, str_len;

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
    };
};

class Client{
public:
    Client(int fd){
        socket_fd = fd;
    }
    int getSockFd() const{
        return socket_fd;
    }
private:
    int socket_fd;
};


class IOepollManager{
    public:
    IOepollManager(int listener_fd):MAX_EVENTS(1024), events(MAX_EVENTS){
        setupEpoll(listener_fd);
    }
    void addToEpoll(int sock_fd){
        event_type.events = EPOLLIN;
        if (epoll_ctl(epoll_fd, EPOLL_CTL_ADD, sock_fd, &event_type) < 0){
            close(epoll_fd);
            throw std::system_error(errno, std::generic_category(), "epoll_ctl() failed");
        }
        
    }

    int watch(){
        event_counts = epoll_wait(epoll_fd, events.data(), MAX_EVENTS, -1);
        if (event_counts < 0){
            throw std::system_error(errno, std::generic_category(), "epoll_wait() failed"); 
        }
        return event_counts;
    }

    const std::vector<struct epoll_event>& getEvents() const{
        return events;
    }

private:
    int epoll_fd;
    int MAX_EVENTS;
    struct epoll_event event_type;
    std::vector<struct epoll_event> events;
    int event_counts = 0;
    void setupEpoll(int listener_fd){
        epoll_fd = epoll_create(1);
        if (epoll_fd < 0){
            throw std::system_error(errno, std::generic_category(), "epoll_create() failed");
        }
        event_type.events = EPOLLIN;
        event_type.data.fd = listener_fd;
        if (epoll_ctl(epoll_fd, EPOLL_CTL_ADD, listener_fd, &event_type) < 0){
            close(epoll_fd);
            close(listener_fd);
            throw std::system_error(errno, std::generic_category(), "epoll_ctl() failed");
        }

    }
};

class ClientManager{
public:
    ClientManager(IOepollManager& io_epoll_manager) : io_epoll_manager(io_epoll_manager){}
    void addClient(int socket_fd){
        std::unique_ptr<Client> client_ptr = std::make_unique<Client>(socket_fd);
        client_map[client_ptr -> getSockFd()] = std::move(client_ptr);
        io_epoll_manager.addToEpoll(socket_fd);
    }

    void removeClient(int client_socket_fd){
        client_map.erase(client_socket_fd);
    }

    void broadCastMsg(int sender_fd, std::string message, int length){
        for (auto iter = client_map.begin(); iter != client_map.end(); iter++){
            if (iter->first != sender_fd){
                write(iter->first, message.c_str(), length);
            }
        }
    }

private:
    IOepollManager& io_epoll_manager;
    std::map<int, std::unique_ptr<Client>> client_map;
};

class Server{
public:
    Server(Listener& listener, ClientManager& client_manager, IOepollManager& io_epoll_manager)
        : listener(listener), 
          client_manager(client_manager), 
          io_epoll_manager(io_epoll_manager){}
    void run(){
        const int BUFFER_SIZE = 1024;
        std::vector<char> buffer(BUFFER_SIZE);

        while(true){
            
            int event_counts = io_epoll_manager.watch();
            const std::vector<struct epoll_event>& events = io_epoll_manager.getEvents();

            for (int i = 0 ; i < event_counts ; i++){
                if (events.at(i).data.fd == listener.getSockFd()){
                    setup_client(listener.getSockFd());
                }
                else{
                    int bytes_read = read(events.at(i).data.fd, buffer.data(), BUFFER_SIZE);
                    if (bytes_read <= 0){
                        std::cout << "Client disconnected: " << events.at(i).data.fd << std::endl;
                        client_manager.removeClient(events.at(i).data.fd);
                    }
                    std::string received_message(buffer.data(), bytes_read);
                    std::string message_to_send = std::to_string(events.at(i).data.fd) + " : " + received_message;
                    client_manager.broadCastMsg(events.at(i).data.fd, message_to_send, message_to_send.length());
                }
            }

        }
    }
private:
    Listener& listener;
    ClientManager& client_manager; 
    IOepollManager& io_epoll_manager;
    void setup_client(int listener_fd){
        int socket_fd;
        socklen_t clnt_addr_size;
        struct sockaddr_in clnt_addr;

        clnt_addr_size = sizeof(clnt_addr);
        socket_fd = accept(listener_fd, (struct sockaddr*)&clnt_addr, &clnt_addr_size);
        if (socket_fd == -1){
            throw std::system_error(errno, std::generic_category(), "accept() failed");
        }
        else{
            client_manager.addClient(socket_fd);
            std::cout << "connection established" << std::endl;
        }
    }

};

int main(int argc, char *argv[]){
    int port = atoi(argv[1]);
    try{
        Listener listener(port);
        IOepollManager io_epoll_manager(listener.getSockFd());
        ClientManager client_manager(io_epoll_manager);
        Server server(listener, client_manager, io_epoll_manager);

        server.run();
    }
    catch (const std::system_error& e) {
        std::cerr << "initialization failed: " << e.what() << " (Code: " << e.code() << ")\n";
        return -1;
    }
    catch (const std::exception& e) {
        std::cerr << "error: " << e.what() << "\n";
        return -1;
    }



}