#ifndef JSON_CONTROLLER_H
#define JSON_CONTROLLER_H

#include <string>

struct JsonData{
public:
    int type;
    std::string who;
    std::string body;

};

class JsonController{
public:
    std::string buildJson(int type, std::string& who, std::string& message);
    int parseTypeFromJson(std::string& input);
    std::string parseWhoFromJson(std::string& input);
    std::string parseBodyFromJson(std::string& input);
};

#endif