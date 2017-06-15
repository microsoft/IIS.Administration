// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core.Utils
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    //
    // Produce stable UUID
    // The cryptography here is NOT intended for security purposes, but for better entropy
    //
    public static class Uuid
    {
        public static byte[] Key { get; set; }

        public static string Encode(string value, string purpose)
        {
            if (Key == null) {
                throw new ArgumentNullException("Key");
            }

            using (var aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = CreateIV(purpose);

                var encryptor = aes.CreateEncryptor();

                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (var bw = new BinaryWriter(cs))
                        {
                            var bytes = Encoding.UTF8.GetBytes(value);
                            bw.Write(bytes);
                        }

                        return Base64.Encode(ms.ToArray());
                    }
                }
            }
        }

        public static string Decode(string uuid, string purpose)
        {
            if (Key == null) {
                throw new ArgumentNullException("Key");
            }

            try {
                var data2 = Base64.Decode(uuid);

                using (var aes = Aes.Create()) {
                    aes.Key = Key;
                    aes.IV = CreateIV(purpose);

                    var decryptor = aes.CreateDecryptor();

                    // Create the streams used for decryption.
                    using (var ms = new MemoryStream(data2))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var buff = new MemoryStream()) {

                        cs.CopyTo(buff);
                        return Encoding.UTF8.GetString(buff.ToArray());
                    }
                }
            }
            catch (FormatException e) {
                throw new NotFoundException(null, e);
            }
            catch (CryptographicException e) {
                throw new NotFoundException(null, e);
            }
        }


        private static byte[] CreateIV(string purpose)
        {
            // Need stable (non-random) IV
            // to produce same UUID for the same value/purpose pair
            //
            byte[] iv = new byte[16];

            using (var hmac = new HMACSHA256(Key))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(purpose));
                Buffer.BlockCopy(hash, 0, iv, 0, iv.Length);
            }

            return iv;
        }
    }
}
