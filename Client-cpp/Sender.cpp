#include "Sender.h"
#include <mutex>
#include <atomic>
#include <winsock2.h>
#include <iostream>
#include <system_error>
#include <cstring>



Sender::Sender(ServerConnector& server_connector, ThreadController& thread_controller) : 
	server_connector(server_connector), 
	thread_controller(thread_controller){}
	
void Sender::watchKeyboard(){
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
	