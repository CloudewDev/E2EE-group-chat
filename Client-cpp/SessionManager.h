#infdef SESSION_MANAGER_H
#define SESSION_MANAGER_H

#include <string>
#include <queue>
#include <vector>
#include <map>
#include <memory>


class Session{
public:
	Session(DoubleRatchet double_ratchet);
private:
	DoubleRatchet double_ratchet;
};

class SessionManager{

public:
	void AddSession(Session session);
	void RemoveSession(std::string id);
private:
	Session server_session;
	std::map<std::string, std::unique_ptr<Session>> session_map;
};

class DoubleRatchet{

public:
	void DHshare();
	void Ratchet();
private:

};

#endif