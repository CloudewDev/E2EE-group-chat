#include "Communicator.h"
#include "JsonController.h"

#include <string>
#include <vector>
#include <unistd.h>

Communicator::Communicator(JsonController& jc) 
    : json_controller(jc) {}

Sender::Sender(JsonController& jc)
    :Communicator(jc) {}

Reciever::Reciever(JsonController& jc)
    :Communicator(jc) {}

std::string Reciever::ReadNBytes(int n, int sock){

    int current_read = 0;
    int bytes_read = 0;
    std::vector<char> buffer(n);

    while (current_read < n){
        bytes_read = read(sock, buffer.data(), n);
        if (bytes_read <= 0){
            return "";
            break;
        } 
        else{
            current_read = current_read + bytes_read;
        }
    }
    std::string recieved_message(buffer.data(), buffer.size());
    return recieved_message;


}