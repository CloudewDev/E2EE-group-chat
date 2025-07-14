#ifndef SHAREDCONTENTS_H
#define SHAREDCONTENTS_H

#include <mutex>
#include <atomic>

struct ThreadController{
	std::mutex cout_mutex;
	std::atomic<bool> program_done{false};
};

struct KeyController{
	std::mutex get_key_mtx;
	int root_key;
	int crypt_key;
};

#endif