#include "DHCalculator.h"

#include <gmp/gmp.h>
#include <string>
#include <ctime> 

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

std::string DHCalculator::CalculateSharedSecret(std::string input){
    mpz_set_str(opponent_num, input.c_str(), 10);
    mpz_powm(shared_secret, opponent_num, x, prime);
    return mpz_get_str(NULL, 10, shared_secret);
}