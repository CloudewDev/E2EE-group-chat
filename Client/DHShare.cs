using System;
using System.Numerics;
using System.Security.Cryptography;

namespace DHShare_ns
{

	public class DHShare
	{
        private BigInteger prime = BigInteger.Parse("32317006071311007300714876688669951960444102669715484032" +
            "13034542752465512312101900047437800025814583420617177669147303598253490428" +
            "75546873115956286388235378759375195778185778053217122993532058804720734560" +
            "98942922814749625084155246078720513006213618212094004222458539598256977332" +
            "21397648331060020541347000021006772701001583240433485642307645601349094213" +
            "47235904972093595995702295002933116041213041748195365487233421073015459402" +
            "268178774054733479303");
        private BigInteger a = 2;
        private BigInteger myPow;
        private BigInteger myValue;

        public BigInteger MakeMyNum()
        {
            myPow = GenerateRandomBigInteger();
            myValue = BigInteger.ModPow(a, myPow, prime);
            return myValue;
        }

        public BigInteger GetSharedSecret(BigInteger input)
        {
            return BigInteger.ModPow(input, myPow, prime);
        }

        private BigInteger GenerateRandomBigInteger()
        {
            byte[] bytes = prime.ToByteArray();
            BigInteger R;
            RandomNumberGenerator r = RandomNumberGenerator.Create();

            do
            {
                r.GetBytes(bytes);
                bytes[bytes.Length - 1] &= (byte)0x7F; //force sign bit to positive
                R = new BigInteger(bytes);
            } while (R > prime - 1);

            return R;
        }
    }

}
