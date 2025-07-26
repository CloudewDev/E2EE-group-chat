#include "JsonController.h"

#include "rapidjson/document.h"
#include "rapidjson/writer.h"
#include "rapidjson/stringbuffer.h"

#include <string>

std::string JsonController::buildJson(int type_input, std::string &from_input, std::string &to_input, std::string &body_input, std::string &iv_input)
{

    rapidjson::Document doc;
    doc.SetObject();

    doc.AddMember("type", type_input, doc.GetAllocator());
    doc.AddMember("from", rapidjson::Value(from_input.c_str(), doc.GetAllocator()), doc.GetAllocator());
    doc.AddMember("to", rapidjson::Value(to_input.c_str(), doc.GetAllocator()), doc.GetAllocator());
    doc.AddMember("body", rapidjson::Value(body_input.c_str(), doc.GetAllocator()), doc.GetAllocator());
    doc.AddMember("iv", rapidjson::Value(iv_input.c_str(), doc.GetAllocator()), doc.GetAllocator());

    rapidjson::StringBuffer buffer;
    rapidjson::Writer<rapidjson::StringBuffer> writer(buffer);
    doc.Accept(writer);

    return buffer.GetString();
}

int JsonController::parseTypeFromJson(std::string &input)
{
    rapidjson::Document d;
    d.Parse(input.c_str());
    return d["type"].GetInt();
}
std::string JsonController::parseFromFromJson(std::string &input)
{
    rapidjson::Document d;
    d.Parse(input.c_str());
    return d["from"].GetString();
}
std::string JsonController::parseToFromJson(std::string &input)
{
    rapidjson::Document d;
    d.Parse(input.c_str());
    return d["to"].GetString();
}
std::string JsonController::parseBodyFromJson(std::string &input)
{

    rapidjson::Document d;
    d.Parse(input.c_str());
    return d["body"].GetString();
}

std::string JsonController::parseIvFromJson(std::string &input)
{

    rapidjson::Document d;
    d.Parse(input.c_str());
    return d["iv"].GetString();
}