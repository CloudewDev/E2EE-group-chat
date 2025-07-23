using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;

using DHShare_ns;


namespace HandshakeStateMachine_ns
{
	public class HandshakeStateMachine
	{
		private DHShare dh_share;
		public HandshakeStateMachine(DHShare ds) 
		{ 
			dh_share = ds;
		}
		public enum STATE {idle, send_handshake, rcv_handshake};
		private string? current_handshaker = "server";
		public string CurrentHandShaker { get { return current_handshaker; } set { current_handshaker = CurrentHandShaker; } }
		private STATE my_state = STATE.idle;
		public STATE MyState { get { return my_state; } }

		private BigInteger my_bignum = 0;
		private BigInteger opponent_bignum = 0;
		private BigInteger shared_secret = 0;
		public string MyBigNum { get { return my_bignum.ToString(); } }
		public string OpponentBigNum { get { return opponent_bignum.ToString(); } set { opponent_bignum = BigInteger.Parse(OpponentBigNum); } }
		public string SharedSecretAsString { get { return shared_secret.ToString(); } }
		public byte[] SharedSecretAsByte { get { return shared_secret.ToByteArray(); } }
		public void SetMyState_ToIdle()
		{
			current_handshaker = null;
			my_bignum = 0;
			opponent_bignum = 0;
			shared_secret = 0;
            my_state = STATE.idle;
        }
		public void SetMyState_ToSendHandShake()
		{
			my_bignum = dh_share.MakeMyNum();
			my_state = STATE.send_handshake;
        }
		public void SetMyState_ToRcvHandShake()
		{
            my_state = STATE.rcv_handshake;
        }

		public void GetSharedSecret(string rcved_bignum)
		{
            opponent_bignum = BigInteger.Parse(rcved_bignum);
            shared_secret = dh_share.GetSharedSecret(opponent_bignum);
            byte[] deriven_key = HKDF.DeriveKey(HashAlgorithmName.SHA3_256, shared_secret.ToByteArray(), 64); //0 ~ 31 is root key, 32 ~ 64 is encryption key
            KEYS keys = new KEYS();
            keys.session_key = deriven_key;
            AddData(current_handshaker, keys);
        }

		public struct KEYS
		{
			public byte[] session_key;
			public byte[] sender_key;
		}
		private Dictionary<string, KEYS> whoNkey_dic = new Dictionary<string, KEYS>();

		public void AddData(string who, KEYS data)
		{
            whoNkey_dic.Add(who, data);
        }
		public void ChangeData(string who, KEYS data)
		{
            whoNkey_dic[who] = data;
		}
        public void RemoveData(string who)
        {
            whoNkey_dic.Remove(who);
        }
		public KEYS AccessData(string who)
		{
			return whoNkey_dic[who];
		}
    }

}
