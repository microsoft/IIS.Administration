// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    class CertificateHelper
    {
        public static async Task<X509Certificate2> Create(string subject, string friendlyName, IEnumerable<string> alternativeNames)
        {
            if (string.IsNullOrEmpty(subject))
            {
                throw new ArgumentNullException(nameof(subject));
            }

            if (string.IsNullOrEmpty(friendlyName))
            {
                throw new ArgumentNullException(nameof(friendlyName));
            }

            string issuer = subject;

            var subjectDn = new CX500DistinguishedName();

            subjectDn.Encode($"CN={subject}", X500NameFlags.XCN_CERT_NAME_STR_NONE);


            var issuerDn = new CX500DistinguishedName();

            issuerDn.Encode($"CN={issuer}", X500NameFlags.XCN_CERT_NAME_STR_NONE);

            var key = new CX509PrivateKey();

            key.ProviderName = "Microsoft RSA SChannel Cryptographic Provider";

            key.Length = 2048;

            //
            // False: Current User, True: Local Machine
            key.MachineContext = true;

            key.Create();

            var cert = new CX509CertificateRequestCertificate();

            cert.InitializeFromPrivateKey(X509CertificateEnrollmentContext.ContextMachine, key, string.Empty);

            cert.Subject = subjectDn;

            cert.Issuer = issuerDn;

            cert.NotBefore = DateTime.UtcNow.AddMinutes(-10);

            cert.NotAfter = cert.NotBefore.AddYears(2);

            var hashAlgorithm = new CObjectId();

            hashAlgorithm.InitializeFromAlgorithmName(ObjectIdGroupId.XCN_CRYPT_FIRST_ALG_OID_GROUP_ID, ObjectIdPublicKeyFlags.XCN_CRYPT_OID_INFO_PUBKEY_ANY, AlgorithmFlags.AlgorithmFlagsNone, "SHA256");

            cert.HashAlgorithm = hashAlgorithm;

            var clientAuthOid = new CObjectId();

            clientAuthOid.InitializeFromValue("1.3.6.1.5.5.7.3.2");

            var serverAuthOid = new CObjectId();

            serverAuthOid.InitializeFromValue("1.3.6.1.5.5.7.3.1");

            var ekuOids = new CObjectIds();

            ekuOids.Add(clientAuthOid);

            ekuOids.Add(serverAuthOid);

            var ekuExt = new CX509ExtensionEnhancedKeyUsage();

            ekuExt.InitializeEncode(ekuOids);

            cert.X509Extensions.Add(ekuExt);

            var keyUsage = new CX509ExtensionKeyUsage();

            var flags = X509KeyUsageFlags.XCN_CERT_KEY_ENCIPHERMENT_KEY_USAGE | X509KeyUsageFlags.XCN_CERT_DIGITAL_SIGNATURE_KEY_USAGE;

            keyUsage.InitializeEncode(flags);

            cert.X509Extensions.Add(keyUsage);

            if (alternativeNames != null)
            {
                var names = new CAlternativeNames();
                var altnames = new CX509ExtensionAlternativeNames();

                foreach (string n in alternativeNames)
                {
                    var name = new CAlternativeName();
                    // Dns Alternative Name
                    name.InitializeFromString(AlternativeNameType.XCN_CERT_ALT_NAME_DNS_NAME, n);
                    names.Add(name);
                }

                altnames.InitializeEncode(names);
                cert.X509Extensions.Add(altnames);
            }

            cert.Encode();

            string locator = Guid.NewGuid().ToString();

            var enrollment = new CX509Enrollment();

            // Get certificates see if any have name of friendlyName
            // if so append (1) or (2) or so forth to friendlyname, even if friendlyname is the empty string

            var friendlyNames = new List<string>();

            using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly);

                foreach (var certificate in store.Certificates)
                {
                    friendlyNames.Add(certificate.FriendlyName);

                    certificate.Dispose();
                }
            }

            int counter = 0;

            string uniqueFriendlyName = friendlyName;

            while (friendlyNames.Any(name => uniqueFriendlyName.Equals(name, StringComparison.OrdinalIgnoreCase))) {
                uniqueFriendlyName = friendlyName + $" ({++counter})";
            }

            friendlyName = uniqueFriendlyName;

            enrollment.CertificateFriendlyName = locator;

            enrollment.InitializeFromRequest(cert);

            string certdata = enrollment.CreateRequest(EncodingType.XCN_CRYPT_STRING_BASE64HEADER);

            enrollment.InstallResponse(InstallResponseRestrictionFlags.AllowUntrustedCertificate, certdata, EncodingType.XCN_CRYPT_STRING_BASE64HEADER, string.Empty);

            X509Certificate2 retCert = null;

            counter = 0;

            //
            // Certificate has been created
            // Wait for the store to be populated and then retrieve the certificate using the guid that it was marked with
            while (retCert == null && counter < 10)
            {
                await Task.Delay(20);

                counter++;

                using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
                {
                    store.Open(OpenFlags.ReadWrite);

                    foreach (var certificate in store.Certificates)
                    {
                        if (certificate.FriendlyName == locator)
                        {
                            retCert = certificate;

                            //
                            // Must update friendlyname while store is still open
                            retCert.FriendlyName = friendlyName;

                            break;
                        }
                    }
                }
            }

            return retCert;
        }
    }
}
