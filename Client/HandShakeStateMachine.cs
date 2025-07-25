using System;
using System.Numerics;
using System.Security.Cryptography;

using DHShare_ns;
using Ratchet_ns;

namespace HandshakeStateMachine_ns
{
	public class HandshakeStateMachine
	{
		private readonly DHShare dh_share;
		private readonly Ratchet ratchet;
		public HandshakeStateMachine(DHShare ds, Ratchet rc) 
		{ 
			dh_share = ds;
			ratchet = rc;
		}
		public enum STATE {idle, send_dh, rcved_dh, waiting_for_rcv_dh};
		private string? current_handshaker = "server";
		public string CurrentHandShaker { get { return current_handshaker; } set { current_handshaker = value; } }
		private STATE my_state = STATE.idle;
		public STATE MyState { get { return my_state; } }

		private BigInteger my_bignum = 0;
		private BigInteger opponent_bignum = 0;
		private BigInteger shared_secret = 0;
		public string MyBigNum { get { return my_bignum.ToString(); } }
		public string OpponentBigNum { get { return opponent_bignum.ToString(); } set { opponent_bignum = BigInteger.Parse(value); } }
		public string SharedSecretAsString { get { return shared_secret.ToString(); } }
		public byte[] SharedSecretAsByte { get { return shared_secret.ToByteArray(); } }
		public void SetMyState_ToIdle()
		{
			GetSharedSecret();
            current_handshaker = null;
			my_bignum = 0;
			opponent_bignum = 0;
			shared_secret = 0;
            my_state = STATE.idle;
            Console.WriteLine("[log]handshake state changed as idle");
        }
		public void SetMyState_ToSendDH()
		{
			if (MyBigNum == "0")
			{
				my_bignum = dh_share.MakeMyNum();
			}
			my_state = STATE.send_dh;
			Console.WriteLine("[log]handshake state changed as send DH");
        }
		public void SetMyState_ToRcvedDH()
		{
            if (MyBigNum == "0")
            {
                my_bignum = dh_share.MakeMyNum();
            }
            my_state = STATE.rcved_dh;
            Console.WriteLine("[log]handshake state changed as rcved DH");
        }

		public void SetMyState_ToWaitingForRcvDH()
		{
			my_state = STATE.waiting_for_rcv_dh;
			Console.WriteLine("[log]handshake state changed as waiting for rcv DH");
		}

		public void GetSharedSecret()
		{
            shared_secret = dh_share.GetSharedSecret(opponent_bignum);
            byte[] deriven_key = HKDF.DeriveKey(HashAlgorithmName.SHA256, shared_secret.ToByteArray(), 64); //0 ~ 31 is root key, 32 ~ 64 is encryption key
			Ratchet.KEYS keys = ratchet.MakeNewKey();
            keys.session_key = deriven_key;
            ratchet.AddData(current_handshaker, keys);
			Console.WriteLine("encoded key is " + Convert.ToBase64String(deriven_key));
        }


    }

}
