#include "Reciever.h"
#include "ServerConnector.h"
#include "SharedContents.h"

#include <vector>
#include <mutex>
#include <atomic>
#include <iostream>
#include <string>
#include <winsock2.h>

Reciever::Reciever(ServerConnector& server_connector, ThreadController& thread_controller):
	server_connector(server_connector),
	thread_controller(thread_controller),
	buffer(BUFFER_SIZE){}
	
void Reciever::watchServer(){
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