#ifndef JSON_CONTROLLER_H
#define JSON_CONTROLLER_H

#include <string>

struct JsonData
{
public:
    int type;
    std::string from;
    std::string to;
    std::string body;
    std::string iv;
};

class JsonController
{
public:
    std::string buildJson(int type, std::string &from, std::string &to, std::string &message, std::string &iv);
    int parseTypeFromJson(std::string &input);
    std::string parseFromFromJson(std::string &input);
    std::string parseToFromJson(std::string &input);
    std::string parseBodyFromJson(std::string &input);
    std::string parseIvFromJson(std::string &input);
};

#endif