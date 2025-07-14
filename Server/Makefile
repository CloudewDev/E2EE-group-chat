CXX = g++
CXXFLAGS = -std=c++17 -Wall -g -pthread
TARGET = chat_server
SRCS = Listener.cpp Client.cpp ClientManager.cpp IOManager.cpp Server.cpp main.cpp

OBJS = $(SRCS:.cpp=.o)

all: $(TARGET)

$(TARGET): $(OBJS)
	$(CXX) $(CXXFLAGS) -o $(TARGET) $(OBJS)

%.o: %.cpp
	$(CXX) $(CXXFLAGS) -c $< -o $@

clean:
	rm -f $(OBJS) $(TARGET)

.PHONY: all clean