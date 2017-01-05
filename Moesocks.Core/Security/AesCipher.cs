using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Moesocks.Security
{
    class AesCipher : Cipher
    {
        private readonly SymmetricAlgorithm _aes;

        public AesCipher(string password, int keySize, int ivSize)
            : base(password, keySize, ivSize)
        {
            _aes = new RijndaelManaged();
            _aes.Mode = CipherMode.CFB;
            _aes.Padding = PaddingMode.Zeros;
            _aes.BlockSize = 128;
            _aes.FeedbackSize = 128;
            _aes.Key = Key;
        }

        public override ICryptoTransform CreateDecryptor(byte[] iv)
        {
            return _aes.CreateDecryptor(_aes.Key, iv);
        }

        public override ICryptoTransform CreateEncryptor(byte[] iv)
        {
            return _aes.CreateEncryptor(_aes.Key, iv);
        }

        public override byte[] GenerateIV()
        {
            var oldIV = _aes.IV;
            _aes.GenerateIV();
            var result = _aes.IV;
            _aes.IV = oldIV;
            return result;
        }
    }
}
