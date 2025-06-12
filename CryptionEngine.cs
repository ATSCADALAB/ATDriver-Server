using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ATDriver_Server
{
    public static class CryptionEngine
    {
        // Sử dụng key riêng cho việc mã hóa/giải mã Address và Value
        private static readonly string privateKey = "ye0UhNl7";

        private static readonly string keyAddress = "cy7aHu2B";

        private static readonly string keyValue = "At6yqU8h";

        // Hàm mã hóa địa chỉ Address
        public static string EncryptAddress(this string input) => Encrypt(input, keyAddress);

        // Hàm giải mã địa chỉ Address
        public static string DecryptAddress(this string input) => Decrypt(input, keyAddress);

        // Hàm mã hóa giá trị Value
        public static string EncryptValue(this string input) => Encrypt(input, keyValue);
        // Hàm mã hóa địa chỉ Address
        public static string DecryptValue(this string input) => Decrypt(input, keyValue);
       
        public static string Encrypt(this string input, string key)
        {            
            var privatekeyByte = Encoding.UTF8.GetBytes(privateKey);           
            var keyByte = Encoding.UTF8.GetBytes(key);
            byte[] inputtextbyteArray = Encoding.UTF8.GetBytes(input);
            using (DESCryptoServiceProvider dsp = new DESCryptoServiceProvider())
            {
                var memstr = new MemoryStream();
                var crystr = new CryptoStream(memstr, dsp.CreateEncryptor(keyByte, privatekeyByte), CryptoStreamMode.Write);
                crystr.Write(inputtextbyteArray, 0, inputtextbyteArray.Length);
                crystr.FlushFinalBlock();
                return Convert.ToBase64String(memstr.ToArray());
            }           
        }

        public static string Decrypt(this string input, string key)
        {                     
            var privatekeyByte = Encoding.UTF8.GetBytes(privateKey);            
            var keyByte = Encoding.UTF8.GetBytes(key);                
            var inputtextbyteArray = Convert.FromBase64String(input.Replace(" ", "+"));
            using (DESCryptoServiceProvider dEsp = new DESCryptoServiceProvider())
            {
                var memstr = new MemoryStream();
                var crystr = new CryptoStream(memstr, dEsp.CreateDecryptor(keyByte, privatekeyByte), CryptoStreamMode.Write);
                crystr.Write(inputtextbyteArray, 0, inputtextbyteArray.Length);
                crystr.FlushFinalBlock();
                return Encoding.UTF8.GetString(memstr.ToArray());
            }            
        }
    }

    
}
