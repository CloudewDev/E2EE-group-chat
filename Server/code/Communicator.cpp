#include "Communicator.h"
#include "JsonController.h"

#include <string>
#include <vector>
#include <unistd.h>
#include <arpa/inet.h>
#include <cstring>
#include <openssl/evp.h>

Communicator::Communicator(JsonController& jc) 
    : json_controller(jc) {}

Sender::Sender(JsonController& jc)
    :Communicator(jc) {}

std::string Sender::MakePacket(int size, std::string message_to_sennd){
    int lengthNetworkOrder = htonl(size);
    std::vector<char> packet(4 + size);
    std::memcpy(packet.data(), &lengthNetworkOrder, 4);
    std::memcpy(packet.data() + 4, message_to_sennd.data(), size);
    std::string data_to_send(packet.begin(), packet.end());
    return data_to_send;
}
std::vector<unsigned char> Sender::encrypt(const unsigned char* plaintext, int plaintext_len,
    const unsigned char* key, const unsigned char* iv){
    
    EVP_CIPHER_CTX* ctx = EVP_CIPHER_CTX_new();
    std::vector<unsigned char> ciphertext(plaintext_len + EVP_MAX_BLOCK_LENGTH);
    int len = 0, ciphertext_len = 0;

    EVP_EncryptInit_ex(ctx, EVP_aes_256_cbc(), NULL, key, iv);
    EVP_EncryptUpdate(ctx, ciphertext.data(), &len, plaintext, plaintext_len);
    ciphertext_len = len;
    EVP_EncryptFinal_ex(ctx, ciphertext.data() + len, &len);
    ciphertext_len += len;

    EVP_CIPHER_CTX_free(ctx);
    ciphertext.resize(ciphertext_len);
    return ciphertext;
}

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

std::vector<unsigned char> decrypt(const unsigned char* ciphertext, int ciphertext_len,
    const unsigned char* key, const unsigned char* iv){

    EVP_CIPHER_CTX* ctx = EVP_CIPHER_CTX_new();
    std::vector<unsigned char> plaintext(ciphertext_len);
    int len = 0, plaintext_len = 0;

    EVP_DecryptInit_ex(ctx, EVP_aes_256_cbc(), NULL, key, iv);
    EVP_DecryptUpdate(ctx, plaintext.data(), &len, ciphertext, ciphertext_len);
    plaintext_len = len;
    EVP_DecryptFinal_ex(ctx, plaintext.data() + len, &len);
    plaintext_len += len;

    EVP_CIPHER_CTX_free(ctx);
    plaintext.resize(plaintext_len);
    return plaintext;
}