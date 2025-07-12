#include <iostream>
#include <cstring>
#include <winsock2.h>
#include <thread>
#include <mutex>
#include <vector>
#include <atomic>

class Sender;
class Reciever;
class ServerConnector;
class Client;


struct ThreadController{
	std::mutex cout_mutex;
	std::atomic<bool> program_done{false};
};

class ServerConnector{
public:
	ServerConnector(char* ip, char* port){
		initializer(ip, port);
	}
	SOCKET getSock() const{
		return sock;
	}

private:
	SOCKET sock;
	
	void initializer(char* ip, char* port){
		SOCKADDR_IN servAdr;
		WSADATA wsaData;
		
    	if (WSAStartup(MAKEWORD(2,2), &wsaData)!=0){
        	throw std::system_error(WSAGetLastError(), std::system_category(), "WSA start up failed");
    	}
		sock = socket(PF_INET, SOCK_STREAM, 0);
    	if (sock == INVALID_SOCKET){
        	throw std::system_error(WSAGetLastError(), std::system_category(), "socket build failed");
    	}
    	memset(&servAdr, 0, sizeof(servAdr));
    	servAdr.sin_family = AF_INET;
    	servAdr.sin_addr.s_addr = inet_addr(ip);
    	servAdr.sin_port = htons(atoi(port));
    	if (connect(sock, (SOCKADDR*)&servAdr, sizeof(servAdr)) == SOCKET_ERROR){
			closesocket(sock);
			throw std::system_error(WSAGetLastError(), std::system_category(), "connection failed");
		}
		else{
			std::cout << "connection established" << std::endl;
		}
	}
};


class Sender{
public:
	Sender(ServerConnector& server_connector, ThreadController& thread_controller) : 
	server_connector(server_connector), 
	thread_controller(thread_controller){}
	
	void watchKeyboard(){
		while(!thread_controller.program_done.load()){
			getline(std::cin, message_to_send);
			
			if (thread_controller.program_done.load()) break;
			
			if (message_to_send == "/quit"){
				thread_controller.program_done.store(true);
				shutdown(server_connector.getSock(), SD_BOTH);
            	closesocket(server_connector.getSock());
				break;
			}
			else{
				if (send(server_connector.getSock(), message_to_send.c_str(), message_to_send.length(), 0) < 0){
					throw std::system_error(WSAGetLastError(), std::system_category(), "send failed");
					std::lock_guard<std::mutex> lock(thread_controller.cout_mutex);
					std::cout << "\r" << std::string(80, ' ') << "\r"; // erase current line
					std::cout << "me : " << message_to_send << std::endl;
				}
			}
		}
	}
	
private:
	ServerConnector& server_connector;
	ThreadController& thread_controller;
	
	std::string message_to_send;
};

//this object will run on multy-thread
class Reciever{
public:
	Reciever(ServerConnector& server_connector, ThreadController& thread_controller):
	server_connector(server_connector),
	thread_controller(thread_controller),
	buffer(BUFFER_SIZE){}
	
	void watchServer(){
		while (!thread_controller.program_done.load()){
			int bytes_read = recv(server_connector.getSock(), buffer.data(), BUFFER_SIZE - 1, 0);
			
			if (bytes_read <= 0){
				std::lock_guard<std::mutex> lock(thread_controller.cout_mutex);
				std::cout << "disconnected" << std::endl;
				thread_controller.program_done.store(true);
				break;
			}
			else{
				std::string received_message(buffer.data(), bytes_read);	
				std::lock_guard<std::mutex> lock(thread_controller.cout_mutex);
				std::cout << "\r" << std::string(80, ' ') << "\r"; // erase current line
				std::cout << "-> " << received_message << std::endl; // and show message
				std::cout << "me : " << std::flush; // then show my state
			}
		}
	}
	
private:
	ThreadController& thread_controller;
	ServerConnector& server_connector;
	const int BUFFER_SIZE = 1024;
	std::vector<char> buffer;
};

class Client{
public:
	Client(ServerConnector& server_connector, Sender& sender, Reciever& reciever, ThreadController& thread_controller) : 
	server_connector(server_connector),
	sender(sender),
	reciever(reciever),
	thread_controller(thread_controller){}
	
	void run(){
    	std::thread receiver_thread(&Reciever::watchServer, &reciever);
    	std::thread sender_thread(&Sender::watchKeyboard, &sender);

	    if (sender_thread.joinable()) {
	        sender_thread.join();
	    }
	    if (receiver_thread.joinable()) {
	        receiver_thread.join();
	    }
    	closesocket(server_connector.getSock());
    	WSACleanup();
    }

	
private:
	ServerConnector& server_connector;
	Sender& sender;
	Reciever& reciever;
	ThreadController& thread_controller;
};
int main(int argc, char** argv) {
	ThreadController thread_controller;
	try{
		ServerConnector server_connector(argv[1], argv[2]);
		Sender sender(server_connector, thread_controller);
		Reciever reciever(server_connector, thread_controller);
		Client client(server_connector, sender, reciever, thread_controller);
		
		client.run();
	}
	catch (const std::system_error& e) {
        std::cerr << "initialization failed: " << e.what() << " (Code: " << e.code() << ")\n";
        return -1;
    }
    catch (const std::exception& e) {
        std::cerr << "error: " << e.what() << "\n";
        return -1;
    }
	return 0;
}