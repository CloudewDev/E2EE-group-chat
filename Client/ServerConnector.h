#ifndef SERVER_CONNECTOR_H
#define SERVER_CONNECTOR_H
#include <winsock2.h>

class ServerConnector{
public:
	ServerConnector(char* ip, char* port);
	SOCKET getSock() const;
private:
	SOCKET sock;
	void initializer(char* ip, char* port);
};

#endif