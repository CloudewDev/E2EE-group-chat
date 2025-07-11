#include <iostream>
#include <sys/socket.h>
#include <arpa/inet.h>
#include <unistd.h>
#include <cstring>
#include <winsock2.h>

class KeyboardIO(){
public:
	
private:	
};

class ServerIO(){
public:
private:
	SOCKET sock;
	void initializer(int ip, int port){
		SOCKADDR_IN servAdr;
		sock = socket(PF_INET, SOCK_STREAM, 0);
    	if (sock == INVALID_SOCKET){
        	throw std::system_error(errno, std::generic_category(), "socket build failed");
    	}
    	memset(&servAdr, 0, sizeof(servAdr));
    	servAdr.sin_family = AF_INET;
    	servAdr.sin_addr.s_addr = inet_addr(ip);
    	servAdr.sin_port = htons(port);
    	if (connect(sock, (SOCKADDR*)&servAdr, sizeof(servAdr)) == SOCKET_ERROR){
			closesocket(sock);
			throw std::system_error(errno, std::generic_category(), "connection failed");
		}
		else{
			std::cout << "connection established" << std::endl;
		}
	}
};

class Client(){
public:
private:
	void initializer(){
		WSADATA wsaData;
    	if (WSAStartup(MAKEWORD(2,2), &wsaData)!=0){
        	throw std::system_error(errno, std::generic_category(), "WSA start up failed");
    	}
	}
};

int main(int argc, char** argv) {
	return 0;
}