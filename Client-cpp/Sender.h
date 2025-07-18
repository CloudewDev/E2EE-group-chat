#ifndef SENDER_H
#define SENDER_H

#include "SharedContents.h"
#include "ServerConnector.h"


class Sender{
public:
	Sender(ServerConnector& server_connector, ThreadController& thread_controller);
	
	void watchKeyboard();
	
private:
	ServerConnector& server_connector;
	ThreadController& thread_controller;
	
	std::string message_to_send;
};

#endif