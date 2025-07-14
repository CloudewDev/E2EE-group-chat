#include "ServerConnector.h"
#include <winsock2.h>
#include <iostream>
#include <system_error>
#include <cstring>
#include <cstdlib>


ServerConnector::ServerConnector(char* ip, char* port){
	initializer(ip, port);
}
SOCKET ServerConnector::getSock() const{
	return sock;
}


void ServerConnector::initializer(char* ip, char* port){
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