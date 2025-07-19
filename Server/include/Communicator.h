#ifndef COMMUNICATOR_H
#define COMMUNICATOR_H

#include "JsonController.h"

#include <string>

class Communicator{
public:
    Communicator(JsonController& jc);

protected:
    JsonController& json_controller;
};

class Sender : Communicator{

public:
    Sender(JsonController& jc);
private:

};

class Reciever : Communicator{
public:
    Reciever(JsonController& jc);

    std::string ReadNBytes(int n, int sock);
private:

};

#endif