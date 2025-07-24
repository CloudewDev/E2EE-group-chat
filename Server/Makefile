CXX = g++
CXXFLAGS = -std=c++17 -Wall -g -pthread -Iinclude -Iinclude/rapidjson -Iinclude/gmp

SRC_DIR = code
OBJ_DIR = build

TARGET = chat_server
SRCS := $(wildcard $(SRC_DIR)/*.cpp)

OBJS := $(patsubst $(SRC_DIR)/%.cpp, $(OBJ_DIR)/%.o, $(SRCS))

all: $(TARGET)

$(TARGET): $(OBJS)
	$(CXX) $(CXXFLAGS) -o $@ $^ ./include/gmp/lib/libgmp.a -lssl -lcrypto

$(OBJ_DIR)/%.o: $(SRC_DIR)/%.cpp
	$(CXX) $(CXXFLAGS) -c $< -o $@

clean:
	rm -f $(OBJS) $(TARGET)

.PHONY: all clean