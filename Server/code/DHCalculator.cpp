#include "DHCalculator.h"

#include <gmp/gmp.h>
#include <string>
#include <ctime> 
#include <vector>
#include <openssl/sha.h>
#include <openssl/bio.h>
#include <openssl/evp.h>
#include <openssl/buffer.h>

DHCalculator::DHCalculator(){
    Init();
}

DHCalculator::~DHCalculator() {
    mpz_clears(prime, a, x, my_num, opponent_num, shared_secret, NULL);
    gmp_randclear(state);
}

void DHCalculator::Init(){
    mpz_inits(prime, a, x, my_num, opponent_num, shared_secret, NULL);
    mpz_set_ui(a, 2);
    mpz_set_str(prime, prime_str.c_str(), 10);
    gmp_randinit_default(state);
    gmp_randseed_ui(state, time(NULL));
}

std::string DHCalculator::SetMyNum(){

    mpz_urandomm(x, state, prime);
    mpz_powm(my_num, a, x, prime);
    return mpz_get_str(NULL, 10, my_num);
}

void DHCalculator::CalculateSharedSecret(std::string input){
    mpz_set_str(opponent_num, input.c_str(), 10);
    mpz_powm(shared_secret, opponent_num, x, prime);
}

void DHCalculator::GetShareSecretByte(std::vector<char>& output){
    size_t guess_size = mpz_sizeinbase(shared_secret, 2) / 8 + 1;
    output.resize(guess_size);

    size_t count = 0;
    mpz_export(output.data(), &count, -1, 1, 1, 0, shared_secret);
    output.resize(count);
}

std::string DHCalculator::base64Encode(const unsigned char* buffer, size_t length) {
    BIO* bio, * b64;
    BUF_MEM* bufferPtr;

    b64 = BIO_new(BIO_f_base64());
    bio = BIO_new(BIO_s_mem());
    bio = BIO_push(b64, bio);

    BIO_set_flags(bio, BIO_FLAGS_BASE64_NO_NL);

    BIO_write(bio, buffer, length);
    BIO_flush(bio);

    BIO_get_mem_ptr(bio, &bufferPtr);
    std::string result(bufferPtr->data, bufferPtr->length);

    BIO_free_all(bio);

    return result;
}