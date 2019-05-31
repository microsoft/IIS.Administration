// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Tests
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using Core.Http;
    using System.IO;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Runtime.InteropServices;

    public class Utils
    {
        public static JObject GetApiKey(string serverUri, HttpClient client)
        {
            string apiKeysUrl = $"{serverUri}/security/api-keys";

            var res = client.GetAsync(apiKeysUrl).Result;

            IEnumerable<string> values;
            res.Headers.TryGetValues(HeaderNames.XSRF_TOKEN, out values);

            if (values.Count() < 1) {
                throw new Exception("Can't get Api Key");
            }

            var body = new {
                expires_on = DateTime.UtcNow.AddMinutes(15)
            };

            string value = values.First();

            HttpContent content = new StringContent(JsonConvert.SerializeObject(body));

            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            content.Headers.Add(HeaderNames.XSRF_TOKEN, value);

            res = client.PostAsync(apiKeysUrl, content).Result;

            JObject key = JsonConvert.DeserializeObject<JObject>(res.Content.ReadAsStringAsync().Result);

            return key;
        }

        public static bool DeleteApiKey(string serverUri, string keyId, HttpClient client)
        {
            string apiKeysUrl = $"{serverUri}/security/api-keys";
            string apiKeyUrl = $"{apiKeysUrl}/{keyId}";

            var res = client.GetAsync(apiKeysUrl).Result;

            IEnumerable<string> values;
            res.Headers.TryGetValues(HeaderNames.XSRF_TOKEN, out values);

            if (values.Count() < 1)
            {
                throw new Exception("Can't delete Api Key");
            }

            string value = values.First();

            HttpRequestMessage message = new HttpRequestMessage(new HttpMethod("DELETE"), apiKeyUrl);
            message.Headers.Add(HeaderNames.XSRF_TOKEN, value);

            res = client.SendAsync(message).Result;

            return Globals.Success(res);
        }

        public static JObject FollowLink(HttpClient client, JObject obj, string linkName)
        {
            string href = obj["_links"]?[linkName]?.Value<string>("href");

            if (href == null) {
                return null;
            }

            string content;
            if (!client.Get($"{Configuration.Instance().TEST_SERVER_URL}{ href }", out content)) {
                return null;
            }

            return JsonConvert.DeserializeObject<JObject>(content);
        }

        public static string Self(JObject obj)
        {
            return GetLink(obj, "self");
        }

        public static string GetLink(JObject obj, string linkName)
        {
            string link = $"{Configuration.Instance().TEST_SERVER_URL}{ obj["_links"][linkName].Value<string>("href") }";

            if (link == null) {
                throw new Exception();
            }
            return link;
        }

        public static JObject ToJ(string value)
        {
            return JsonConvert.DeserializeObject<JObject>(value);
        }

        public static JObject GetFeature(HttpClient client, string url, string siteName, string path)
        {
            if (path != null) {
                if (!path.StartsWith("/")) {
                    throw new ArgumentException("path");
                }
            }

            string content;
            if (!client.Get(url + "?scope=" + siteName + path, out content)) {
                return null;
            }

            return ToJ(content);
        }

        public static bool JEquals<T>(JObject one, JObject two, string propertyName, StringComparison comparisonType = StringComparison.CurrentCulture)
        {
            var parts = propertyName.Split('.');

            for (int i = 0; i < parts.Length - 1; i++) {
                one = one.Value<JObject>(parts[i]);
                two = two.Value<JObject>(parts[i]);
            }

            int name = parts.Length - 1;

            if (typeof(T) == typeof(string)) {
                return one.Value<string>(parts[name]).Equals(two.Value<string>(parts[name]), comparisonType);
            }

            return one.Value<T>(parts[name]).Equals(two.Value<T>(parts[name]));
        }

        public static void InitializeTestEnvironment(string rootPath)
        {
            string testSiteName = "test_site";
            string testAppName = "test_application";
            string testVdirName = "test_vdir";
            string testDirectoryName = "test_directory";

            if(!Directory.Exists(rootPath))
            {
                Directory.CreateDirectory(rootPath);
            }

            string sitePath = Path.Combine(rootPath, testSiteName);

            if(!Directory.Exists(sitePath))
            {
                Directory.CreateDirectory(sitePath);
            }

            var appPath = Path.Combine(sitePath, testAppName);

            if(!Directory.Exists(appPath))
            {
                Directory.CreateDirectory(appPath);
            }

            var vdirPath = Path.Combine(sitePath, testVdirName);

            if (!Directory.Exists(vdirPath))
            {
                Directory.CreateDirectory(vdirPath);
            }

            var dirPath = Path.Combine(sitePath, testDirectoryName);

            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
        }

        public static int GetAvailablePort()
        {
            int current = 30000;

            do {
                if (IsPortAvailable(current)) {
                    return current;
                }
                current++;
            } while (current < IPEndPoint.MaxPort);

            throw new FileNotFoundException();
        }

        public static bool IsPortAvailable(int port)
        {
            var listener = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Where(l => l.Port == port).FirstOrDefault();
            return listener == null;
        }

        public static Version OsVersion {
            get {
                string osOutput = RuntimeInformation.OSDescription.Trim();
                return Version.Parse(osOutput.Substring(osOutput.IndexOfAny(new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' })));
            }
        }
    }
}
