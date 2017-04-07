// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.CentralCertificates
{
    class Constants
    {
        public const string STORE_NAME = "IIS Central Certificate Store";

        public const string REGKEY_CENTRAL_CERTIFICATE_STORE_PROVIDER = "SOFTWARE\\Microsoft\\IIS\\CentralCertProvider";
        public const string REGVAL_CERT_STORE_LOCATION = "CertStoreLocation";
        public const string REGVAL_USERNAME = "UserName";
        public const string REGVAL_PASSWORD = "Password";
        public const string REGVAL_PRIVATE_KEY_PASSWORD = "PrivateKeyPassword";
        public const string REGVAL_POLLING_INTERVAL = "PollingInterval";
        public const string REGVAL_ENABLED = "Enabled";
        public const int DEFAULT_POLLING_INTERVAL = 300; // seconds
    }
}
