#ifndef DH_CALCULATOR_H
#define DH_CALCULATOR_H

#include <gmp/gmp.h>
#include <string>
#include <vector>

class DHCalculator{

public:
    DHCalculator();
    ~DHCalculator();
    void Init();
    std::string SetMyNum();
    void CalculateSharedSecret(std::string input);
    void GetShareSecretByte(std::vector<char>& output);
    std::string base64Encode(const unsigned char* buffer, size_t length);

private:
    std::string prime_str = "32317006071311007300714876688669951960444102669715484032"
            "13034542752465512312101900047437800025814583420617177669147303598253490428"
            "75546873115956286388235378759375195778185778053217122993532058804720734560"
            "98942922814749625084155246078720513006213618212094004222458539598256977332"
            "21397648331060020541347000021006772701001583240433485642307645601349094213"
            "47235904972093595995702295002933116041213041748195365487233421073015459402"
            "268178774054733479303";
    mpz_t prime, a, x, my_num, opponent_num, shared_secret;
    gmp_randstate_t state;

};



#endif