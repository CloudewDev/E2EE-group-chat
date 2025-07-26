#include "DHCalculator.h"

#include <gmp/gmp.h>
#include <string>
#include <ctime>
#include <vector>
#include <openssl/sha.h>
#include <openssl/bio.h>
#include <openssl/evp.h>
#include <openssl/buffer.h>
#include <openssl/kdf.h>
#include <system_error>
#include <iostream>

DHCalculator::DHCalculator()
{
    Init();
}

DHCalculator::~DHCalculator()
{
    mpz_clears(prime, a, x, my_num, opponent_num, shared_secret, NULL);
    gmp_randclear(state);
}

void DHCalculator::Init()
{
    mpz_inits(prime, a, x, my_num, opponent_num, shared_secret, NULL);
    mpz_set_ui(a, 2);
    mpz_set_str(prime, prime_str.c_str(), 10);
    gmp_randinit_default(state);
    gmp_randseed_ui(state, time(NULL));
}

std::string DHCalculator::SetMyNum()
{

    mpz_urandomm(x, state, prime);
    mpz_powm(my_num, a, x, prime);
    return mpz_get_str(NULL, 10, my_num);
}

void DHCalculator::CalculateSharedSecret(std::string input)
{
    mpz_set_str(opponent_num, input.c_str(), 10);
    mpz_powm(shared_secret, opponent_num, x, prime);
}

void DHCalculator::GetShareSecretByte(std::vector<unsigned char> &input)
{
    size_t guess_size = mpz_sizeinbase(shared_secret, 2) / 8 + 1;
    input.resize(guess_size);

    size_t count = 0;
    mpz_export(input.data(), &count, -1, 1, 1, 0, shared_secret);
    input.resize(count);
}


//just typical use of openssl bio
std::string DHCalculator::base64Encode(const unsigned char *buffer, size_t length)
{
    BIO *bio, *b64;
    BUF_MEM *bufferPtr;

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


//just typical us of openssl key drivation function
std::vector<unsigned char> DHCalculator::GetKey()
{
    std::vector<unsigned char> shared_secret_as_byte;
    GetShareSecretByte(shared_secret_as_byte);

    size_t output_len = 64;
    std::vector<unsigned char> okm(output_len);

    EVP_PKEY_CTX *pctx = EVP_PKEY_CTX_new_id(EVP_PKEY_HKDF, nullptr);
    if (!pctx)
        throw std::runtime_error("Failed to create HKDF context");

    if (EVP_PKEY_derive_init(pctx) <= 0)
    {
        EVP_PKEY_CTX_free(pctx);
        throw std::runtime_error("EVP_PKEY_derive_init failed");
    }
    if (EVP_PKEY_CTX_set_hkdf_md(pctx, EVP_sha256()) <= 0)
    {
        EVP_PKEY_CTX_free(pctx);
        throw std::runtime_error("EVP_PKEY_CTX_set_hkdf_md failed");
    }

    if (EVP_PKEY_CTX_set1_hkdf_key(pctx, shared_secret_as_byte.data(), shared_secret_as_byte.size()) <= 0)
    {
        EVP_PKEY_CTX_free(pctx);
        throw std::runtime_error("EVP_PKEY_CTX_set1_hkdf_key failed");
    }
    if (EVP_PKEY_CTX_set1_hkdf_salt(pctx, nullptr, 0) <= 0)
    {
        EVP_PKEY_CTX_free(pctx);
        throw std::runtime_error("EVP_PKEY_CTX_set1_hkdf_salt failed");
    }
    if (EVP_PKEY_CTX_add1_hkdf_info(pctx, nullptr, 0) <= 0)
    {
        EVP_PKEY_CTX_free(pctx);
        throw std::runtime_error("VP_PKEY_CTX_add1_hkdf_info failed");
    }
    if (EVP_PKEY_derive(pctx, okm.data(), &output_len) <= 0)
    {
        EVP_PKEY_CTX_free(pctx);
        throw std::runtime_error("HKDF derivation failed");
    }

    EVP_PKEY_CTX_free(pctx);
    return okm;
}