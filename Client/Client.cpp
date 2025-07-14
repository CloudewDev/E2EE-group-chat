#include "ServerConnector.h"
#include "SharedContents.h"
#include "Sender.h"
#include "Reciever.h"
#include "Client.h"

#include <cstring>
#include <winsock2.h>
#include <thread>

Client::Client(ServerConnector& server_connector, Sender& sender, Reciever& reciever, ThreadController& thread_controller) : 
	server_connector(server_connector),
	sender(sender),
	reciever(reciever),
	thread_controller(thread_controller){}
	
void Client::run(){
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
