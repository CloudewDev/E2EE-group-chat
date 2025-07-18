#ifndef CLIENT_H
#define CLIENT_H

#include "ServerConnector.h"
#include "SharedContents.h"
#include "Sender.h"
#include "Reciever.h"

class Client{
public:
	Client(ServerConnector& server_connector, Sender& sender, Reciever& reciever, ThreadController& thread_controller);
	void run();
	
private:
	ServerConnector& server_connector;
	Sender& sender;
	Reciever& reciever;
	ThreadController& thread_controller;
};

#endif