// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Tests
{
    using Win32.SafeHandles;
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using Xunit.Abstractions;
    using Newtonsoft.Json.Linq;
    using Xunit;
    using System.Net.Http;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.Linq;
    using System.Diagnostics;
    using System.Threading.Tasks;

    // NOTE: This test intermittently fails because it tries to disable/enable Windows Features. Details: https://github.com/Microsoft/IIS.Administration/issues/236
    public class CentralCertificates
    {
        private static readonly string CERTIFICATES_API_PATH = $"{Configuration.Instance().TEST_SERVER_URL}/api/certificates";
        private static readonly string STORES_API_PATH = $"{Configuration.Instance().TEST_SERVER_URL}/api/certificates/stores";
        private static readonly string FOLDER_PATH = Path.Combine(Configuration.Instance().TEST_ROOT_PATH, FOLDER_NAME);
        private const string NAME = "IIS Central Certificate Store";
        private const string FOLDER_NAME = "CentralCertStore";
        private const string CERT_NAME = "IISAdminLocalTest";
        private const string PVK_PASS = "abcdefg";
        private ITestOutputHelper _output;

        public CentralCertificates(ITestOutputHelper output)
        {
            _output = output;
        }

        public static string CcsTestUsername {
            get {
                return Configuration.Instance().CCSUser;
            }
        }

        [Fact]
        public async Task CanEnable()
        {
            RequireCcsTestInfrastructure();
            CcsUser user = await CcsUser.Get();

            Assert.True(Disable());
            Assert.True(Enable(FOLDER_PATH, user.Username, user.Password, PVK_PASS));
        }

        [Fact]
        public async Task PathMustBeAllowed()
        {
            RequireCcsTestInfrastructure();
            const string path = @"C:\Not\Allowed\Path";

            Assert.True(Disable());

            CcsUser user = await CcsUser.Get();

            dynamic ccsInfo = new
            {
                path = path,
                identity = new
                {
                    username = user.Username,
                    password = user.Password
                },
                private_key_password = PVK_PASS
            };

            using (var client = ApiHttpClient.Create()) {
                JObject webserver = client.Get($"{Configuration.Instance().TEST_SERVER_URL}/api/webserver/");
                string ccsLink = Utils.GetLink(webserver, "central_certificates");
                HttpResponseMessage res = client.PostRaw(ccsLink, (object)ccsInfo);
                Assert.True((int)res.StatusCode == 403);
            }
        }

        [Fact]
        public void CredentialsMustBeValid()
        {
            RequireCcsTestInfrastructure();
            Assert.True(Disable());

            dynamic ccsInfo = new
            {
                path = FOLDER_PATH,
                identity = new
                {
                    username = CcsTestUsername,
                    password = "fgsfds"
                },
                private_key_password = PVK_PASS
            };

            using (var client = ApiHttpClient.Create()) {
                JObject webserver = client.Get($"{Configuration.Instance().TEST_SERVER_URL}/api/webserver/");
                string ccsLink = Utils.GetLink(webserver, "central_certificates");
                HttpResponseMessage res = client.PostRaw(ccsLink, (object)ccsInfo);
                Assert.True((int)res.StatusCode == 400);
                Assert.True(res.Content.Headers.ContentType.ToString().Contains("json"));
                JObject apiError = JsonConvert.DeserializeObject<JObject>(res.Content.ReadAsStringAsync().Result);
                Assert.True(apiError.Value<string>("name").Equals("identity"));
            }
        }

        [Fact]
        public async Task DynamicallyAddsToStores()
        {
            RequireCcsTestInfrastructure();
            CcsUser user = await CcsUser.Get();

            Assert.True(Disable());
            Assert.False(GetStores().Any(store => store.Value<string>("name").Equals(NAME, StringComparison.OrdinalIgnoreCase)));
            Assert.True(Enable(FOLDER_PATH, user.Username, user.Password, PVK_PASS));
            Assert.True(GetStores().Any(store => store.Value<string>("name").Equals(NAME, StringComparison.OrdinalIgnoreCase)));
            Assert.True(Disable());
            Assert.False(GetStores().Any(store => store.Value<string>("name").Equals(NAME, StringComparison.OrdinalIgnoreCase)));
        }

        [Fact]
        public async Task CcsCertificatesShown()
        {
            RequireCcsTestInfrastructure();
            CcsUser user = await CcsUser.Get();

            Assert.True(Enable(FOLDER_PATH, user.Username, user.Password, PVK_PASS));
            Assert.True(GetCertificates().Any(cert => {
                return cert.Value<string>("alias").Equals(CERT_NAME + ".pfx") &&
                    cert.Value<JObject>("store").Value<string>("name").Equals(NAME, StringComparison.OrdinalIgnoreCase);
            }));
        }

        [Fact]
        public async Task CanCreateCcsBinding()
        {
            RequireCcsTestInfrastructure();
            CcsUser user = await CcsUser.Get();

            Assert.True(Enable(FOLDER_PATH, user.Username, user.Password, PVK_PASS));

            JObject site;
            const string siteName = "CcsBindingTestSite";
            using (var client = ApiHttpClient.Create()) {
                Sites.EnsureNoSite(client, siteName);
                site = Sites.CreateSite(_output, client, siteName, Utils.GetAvailablePort(), Sites.TEST_SITE_PATH);
                Assert.NotNull(site);

                try {
                    JObject cert = GetCertificates().FirstOrDefault(c => {
                        return c.Value<string>("alias").Equals(CERT_NAME + ".pfx") &&
                            c.Value<JObject>("store").Value<string>("name").Equals(NAME, StringComparison.OrdinalIgnoreCase);
                    });
                    Assert.NotNull(cert);

                    site["bindings"] = JToken.FromObject(new object[] {
                        new {
                            port = 443,
                            protocol = "https",
                            ip_address = "*",
                            hostname = CERT_NAME,
                            certificate = cert,
                            require_sni = true
                        }
                    });

                    site = client.Patch(Utils.Self(site), site);
                    Assert.NotNull(site);

                    string index = Path.Combine(site.Value<string>("physical_path"), "index.html");
                    if (!File.Exists(index)) {
                        File.WriteAllText(index, $"<h1>{siteName}</h1>");
                    }

                    site = client.Get(Utils.Self(site));

                    JObject binding = site["bindings"].ToObject<IEnumerable<JObject>>().First();
                    Assert.NotNull(binding["certificate"]);
                    Assert.True(binding.Value<bool>("require_sni"));

                    JObject certificate = client.Get(Utils.Self(binding.Value<JObject>("certificate")));
                    Assert.NotNull(certificate);
                    Assert.True(certificate["store"].Value<string>("name").Equals(NAME));
                }
                finally {
                    Sites.EnsureNoSite(client, siteName);
                }
            }
        }



        private bool Enable(string physicalPath, string username, string password, string privateKeyPassword)
        {
            dynamic ccsInfo = new
            {
                path = physicalPath,
                identity = new
                {
                    username = username,
                    password = password
                },
                private_key_password = privateKeyPassword
            };

            using (var client = ApiHttpClient.Create()) {
                JObject webserver = client.Get($"{Configuration.Instance().TEST_SERVER_URL}/api/webserver/");
                string ccsLink = Utils.GetLink(webserver, "central_certificates");
                return client.Post(ccsLink, (object)ccsInfo) != null;
            }
        }

        private bool Disable()
        {
            using (var client = ApiHttpClient.Create()) {
                JObject webserver = client.Get($"{Configuration.Instance().TEST_SERVER_URL}/api/webserver/");
                string ccsLink = Utils.GetLink(webserver, "central_certificates");
                return client.Delete(ccsLink);
            }
        }

        private JObject GetCcs()
        {
            using (var client = ApiHttpClient.Create()) {
                JObject webserver = client.Get($"{Configuration.Instance().TEST_SERVER_URL}/api/webserver/");
                return Utils.FollowLink(client, webserver, "central_certificates");
            }
        }

        private IEnumerable<JObject> GetStores()
        {
            using (var client = ApiHttpClient.Create()) {
                JObject containingObject = client.Get(STORES_API_PATH);
                return containingObject["stores"].ToObject<IEnumerable<JObject>>();
            }
        }

        private IEnumerable<JObject> GetCertificates()
        {
            using (var client = ApiHttpClient.Create()) {
                JObject containingObject = client.Get(CERTIFICATES_API_PATH + "?fields=*");
                return containingObject["certificates"].ToObject<IEnumerable<JObject>>();
            }
        }

        private void RequireCcsTestInfrastructure()
        {
            if (!Directory.Exists(FOLDER_PATH)) {
                _output.WriteLine("Ccs test folder not found");
                throw new Exception();
            }

            string[] certs = Directory.GetFiles(FOLDER_PATH, "*.pfx");
            if (certs.Length == 0) {
                _output.WriteLine("Ccs test certificates not found");
                throw new Exception();
            }
        }

        private static bool LocalUserExists(string username, string password)
        {
            SafeAccessTokenHandle token;

            string[] parts = username.Split('\\');
            string domain = null;

            if (parts.Length > 1) {
                domain = parts[0];
                username = parts[1];
            }
            else {
                domain = ".";
                username = parts[0];
            }

            bool loggedOn = Interop.LogonUserExExW(username,
                domain,
                password,
                Interop.LOGON32_LOGON_INTERACTIVE,
                Interop.LOGON32_PROVIDER_DEFAULT,
                IntPtr.Zero,
                out token,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);

            token.Dispose();
            return loggedOn;
        }



    }

    class Interop
    {
        private const string SECURITY_API_SET = "sspicli.dll";

        public const int LOGON32_PROVIDER_DEFAULT = 0;
        public const int LOGON32_LOGON_INTERACTIVE = 2;

        [DllImport(SECURITY_API_SET, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool LogonUserExExW(
            string lpszUsername,
            string lpszDomain,
            string lpszPassword,
            int dwLogonType,
            int dwLogonProvider,
            IntPtr pTokenGroups,
            out SafeAccessTokenHandle phToken,
            IntPtr ppLogonSid,
            IntPtr ppProfileBuffer,
            IntPtr pdwProfileLength,
            IntPtr pQuotaLimits);
    }

    class CcsUser
    {
        private CcsUser() { }

        public string Username { get; set; }
        public string Password { get; set; }

        public static async Task<CcsUser> Get()
        {
            var user = new CcsUser();

            user.Username = CentralCertificates.CcsTestUsername;
            user.Password = Guid.NewGuid().ToString();
            await CreateLocalUser(user.Username, user.Password);

            return user;
        }

        private static Task CreateLocalUser(string username, string password)
        {
            var createScriptLocation = Path.Combine(Configuration.Instance().PROJECT_PATH, "scripts", "tests", "Create-User.ps1");
            // User creation already implemented in powershell install scripts, vs many interop calls
            return RunProcess("PowerShell.exe", $@"""{createScriptLocation}"" -Name '{username}' -Password '{password}'");
        }

        private static Task RunProcess(string tool, string arguments)
        {
            ProcessStartInfo info = new ProcessStartInfo(tool, arguments);

            Process p = new Process()
            {
                StartInfo = info,
                EnableRaisingEvents = true
            };

            var tcs = new TaskCompletionSource<int>();

            p.Exited += (sender, args) => {
                if (p.ExitCode != 0) {
                    tcs.SetException(new Exception($"Process exited with an error: {p.ExitCode}"));
                }
                else {
                    tcs.SetResult(p.ExitCode);
                }
                p.Dispose();
            };

            p.Start();
            return tcs.Task;
        }
    }
}
