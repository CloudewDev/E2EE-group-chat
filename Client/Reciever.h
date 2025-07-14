#ifndef RECIEVER_H
#define RECIEVER_H

#include "ServerConnector.h"
#include "SharedContents.h"

#include <vector>


class Reciever{
public:
	Reciever(ServerConnector& server_connector, ThreadController& thread_controller);
	void watchServer();
	
private:
	ThreadController& thread_controller;
	ServerConnector& server_connector;
	const int BUFFER_SIZE = 1024;
	std::vector<char> buffer;
};

#endif