using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Moesocks.Security
{
    public abstract class Cipher
    {
        public byte[] Key { get; }
        public int KeySize { get; }
        public int IVSize { get; }

        protected Cipher(string password, int keySize, int ivSize)
        {
            KeySize = keySize;
            IVSize = ivSize;

            Key = ComputeKey(password);
        }
        
        public abstract ICryptoTransform CreateDecryptor(byte[] iv);
        public abstract ICryptoTransform CreateEncryptor(byte[] iv);
        public abstract byte[] GenerateIV();

        private byte[] ComputeKey(string password)
        {
            var key = new byte[KeySize];
            var hashPass = Encoding.UTF8.GetBytes(password);
            var tmp = new byte[hashPass.Length + 16];
            int i = 0;
            byte[] tmp2 = null;
            while (i < KeySize)
            {
                if (i == 0)
                    tmp2 = _md5.ComputeHash(hashPass);
                else
                {
                    tmp2.CopyTo(tmp, 0);
                    hashPass.CopyTo(tmp, tmp2.Length);
                    tmp2 = _md5.ComputeHash(tmp);
                }
                tmp2.CopyTo(key, i);
                i += tmp2.Length;
            }
            return key;
        }

        public static Cipher Create(string name, string password)
        {
            return new AesCipher(password, 32, 16);
        }

        private readonly MD5 _md5 = MD5.Create();

        private byte[] HashPassword(string password)
        {
            var src = Encoding.UTF8.GetBytes(password);
            return _md5.ComputeHash(src);
        }
    }
}
