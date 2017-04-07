// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.CentralCertificates
{
    using System;
    using System.Security.Cryptography;
    using System.Text;

    public class Crypto
    {
        private const string WAS_KEY_CONTAINER = "iisWasKey";
        private const int PROV_RSA_AES = 24;

        public static byte[] Encrypt(string value)
        {
            byte[] bytes = RsaEncrypt(value);

            //
            // Keep compatibility with with the Microsoft Cryptographic API (CAPI)
            // Managed RSA Crypto provider produces bytes in reverse order with respect to CryptEncrypt
            // "To interoperate with CAPI, you must manually reverse the order of encrypted bytes before the encrypted data interoperates with another API." - msdn
            Array.Reverse(bytes);
            return bytes;
        }

        public static string Decrypt(byte[] encrypted)
        {
            //
            // Reverse bytes to interop with Microsoft Cryptographic Api
            Array.Reverse(encrypted);
            return RsaDecrypt(encrypted);
        }

        private static byte[] RsaEncrypt(string value)
        {
            using (var provider = AcquireCryptoProvider()) {
                return provider.Encrypt(Encoding.Unicode.GetBytes(value), RSAEncryptionPadding.Pkcs1);
            }
        }

        private static string RsaDecrypt(byte[] encrypted)
        {
            using (var provider = AcquireCryptoProvider()) {
                byte[] bytes = provider.Decrypt(encrypted, RSAEncryptionPadding.Pkcs1);
                return Encoding.Unicode.GetString(bytes);
            }
        }

        private static RSACryptoServiceProvider AcquireCryptoProvider()
        {
            var providerParams = new CspParameters();

            providerParams.KeyContainerName = WAS_KEY_CONTAINER;
            providerParams.ProviderType = PROV_RSA_AES;
            providerParams.Flags |= CspProviderFlags.UseMachineKeyStore;

            var prov = new RSACryptoServiceProvider(providerParams);
            return prov;
        }
    }
}
