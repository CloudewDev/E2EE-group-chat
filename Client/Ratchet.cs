using HandshakeStateMachine_ns;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using static HandshakeStateMachine_ns.HandshakeStateMachine;

namespace Ratchet_ns
{

	public class Ratchet
	{
        public enum STATE {idle, send_key, rcved_key, waiting_for_rcv_key }
        private STATE my_state;
        public STATE MyState { get {  return my_state; } }
        private string? current_exchanger = null;
        public string CurrentExchanger { get { return current_exchanger; } set { current_exchanger = value; } }
        public Ratchet()
        {
            using RandomNumberGenerator r = RandomNumberGenerator.Create();
            r.GetBytes(my_sender_key);
        }
        private byte[] my_sender_key = new byte[32];
        public byte[] MySenderKey { get { return my_sender_key; } }
		public int UpdateSessionKey(string who)
		{
            KEYS temp = whoNkey_dic[who];
            byte[] root_key = new byte[32];
            Array.Copy(temp.session_key, 0, root_key, 32, 32);
            temp.session_key = HKDF.DeriveKey(HashAlgorithmName.SHA256, root_key, 64);

            if (++temp.count == 3)
            {
                temp.count = 0;
                whoNkey_dic[who] = temp;
                return 1;
            }
            else
            {
                whoNkey_dic[who] = temp;
                return 0;
            }
            
        }

        public void SetOpponentSenderKey (byte[] iv, byte[] encrypted_key)
        {
            Console.WriteLine("[log]recieved opponent's sender key");
            byte[] key = new byte[32];
            Array.Copy(whoNkey_dic[current_exchanger].session_key, 0, key, 0, 32);
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            KEYS temp = whoNkey_dic[current_exchanger];
            temp.sender_key = decryptor.TransformFinalBlock(encrypted_key, 0, encrypted_key.Length);
            whoNkey_dic[current_exchanger] = temp;
        }

        public void UpdateMySenderKey()
        {
            using RandomNumberGenerator r = RandomNumberGenerator.Create();
            r.GetBytes(my_sender_key);
        }
        public void SetMyState_ToIdle()
        {
            current_exchanger = null;
            my_state = STATE.idle;
            Console.WriteLine("[log]ratchet state changed as idle");
        }
        public void SetMyState_ToSendKey()
        {
            my_state = STATE.send_key;
            Console.WriteLine("[log]ratchet state changed as send key");

        }

        public void SetMyState_ToRcvedKey()
        {
            my_state = STATE.rcved_key;
            Console.WriteLine("[log]ratchet state changed as rcved key");
        }

        public void SetMyState_ToWaitingForRcvKey()
        {
            my_state = STATE.waiting_for_rcv_key;
            Console.WriteLine("[log]ratchet state changed as waiting for rcv key");
        }

        public struct KEYS
        {
            public byte[]? session_key;
            public byte[]? sender_key;
            public int count;
        }
        private Dictionary<string, KEYS> whoNkey_dic = new Dictionary<string, KEYS>();

        public KEYS MakeNewKey()
        {
            KEYS output = new KEYS();
            output.session_key = null;
            output.sender_key = null;
            output.count = 0;
            return output;
        }

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

