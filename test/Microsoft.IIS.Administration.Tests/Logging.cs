// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Tests
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using Xunit;

    public class Logging
    {
        public static readonly string LOGGING_URL = $"{Configuration.Instance().TEST_SERVER_URL}/api/webserver/logging";

        [Fact]
        public void TestCustomFields()
        {
            using (HttpClient client = ApiHttpClient.Create())
            {
                // Web Server Scope
                JObject webServerFeature = GetLoggingFeature(client, null, null);
                JObject cachedFeature = new JObject(webServerFeature);
                string result;

                try {

                    webServerFeature["log_per_site"] = true;
                    webServerFeature["log_file_format"] = "w3c";

                    Assert.True(client.Patch(Utils.Self(webServerFeature), JsonConvert.SerializeObject(webServerFeature), out result));
                    webServerFeature = JsonConvert.DeserializeObject<JObject>(result);
                    JObject cachedPerSiteSettings = new JObject(webServerFeature);

                    try {

                        JArray customFields = webServerFeature.Value<JArray>("custom_log_fields");

                        JObject custField = JObject.FromObject(new {
                            field_name = "Test_Field",
                            source_type = "request_header",
                            source_name = "Test-Field"
                        });

                        customFields.Add(custField);

                        Assert.True(client.Patch(Utils.Self(webServerFeature), JsonConvert.SerializeObject(webServerFeature), out result));

                        JObject patchedFeature = JsonConvert.DeserializeObject<JObject>(result);

                        customFields = patchedFeature.Value<JArray>("custom_log_fields");

                        Assert.True(JToken.DeepEquals(custField, customFields[customFields.Count - 1]));
                    }
                    finally {
                        Assert.True(client.Patch(Utils.Self(webServerFeature), JsonConvert.SerializeObject(cachedPerSiteSettings), out result));
                    }

                }
                finally {
                    Assert.True(client.Patch(Utils.Self(webServerFeature), JsonConvert.SerializeObject(cachedFeature), out result));
                }
            }
        }

        [Fact]
        public void ChangeAllProperties()
        {
            var testLogDirectoryPath = Path.Combine(@"%systemdrive%\inetpub", "logstest");

            if (!Directory.Exists(Environment.ExpandEnvironmentVariables(testLogDirectoryPath))) {
                Directory.CreateDirectory(Environment.ExpandEnvironmentVariables(testLogDirectoryPath));
            }

            using (HttpClient client = ApiHttpClient.Create())
            {
                // Web Server Scope
                JObject feature = GetLoggingFeature(client, null, null);
                JObject cachedFeature = new JObject(feature);

                string result;

                List<LogFormat> logFormats = new List<LogFormat>() {
                        new LogFormat() {
                            Name = "w3c",
                            IsServerLevel = true
                        },
                        new LogFormat() {
                            Name = "binary",
                            IsServerLevel = true
                        },
                        new LogFormat() {
                            Name = "w3c",
                            IsServerLevel = false
                        },
                    };

                try {
                    foreach (var target in logFormats)
                    {
                        JObject rollover = feature.Value<JObject>("rollover");

                        feature["log_per_site"] = !target.IsServerLevel;
                        feature["log_file_format"] = target.Name;

                        Assert.True(client.Patch(Utils.Self(feature), JsonConvert.SerializeObject(feature), out result));
                        JObject uFeature = JsonConvert.DeserializeObject<JObject>(result);                        
                        JObject cachedSpecific = new JObject(uFeature);

                        Assert.True(Utils.JEquals<bool>(feature, uFeature, "log_per_site"));
                        Assert.True(Utils.JEquals<string>(feature, uFeature, "log_file_format"));

                        feature = uFeature;

                        try {
                            feature["enabled"] = !feature.Value<bool>("enabled");
                            feature["log_file_encoding"] = feature.Value<string>("log_file_encoding") == "utf-8" ? "ansi" : "utf-8";

                            feature["directory"] = testLogDirectoryPath;

                            rollover["period"] = rollover.Value<string>("period") == "daily" ? "weekly" : "daily";
                            rollover["truncate_size"] = rollover.Value<long>("truncate_size") - 1;
                            rollover["local_time_rollover"] = !rollover.Value<bool>("local_time_rollover");

                            if (target.Name == "w3c" && !target.IsServerLevel) {
                                JObject logTarget = feature.Value<JObject>("log_target");
                                logTarget["etw"] = !logTarget.Value<bool>("etw");
                                logTarget["file"] = !logTarget.Value<bool>("file");
                            }

                            if(target.Name == "w3c") {
                                JObject logFields = feature.Value<JObject>("log_fields");
                                IList<string> keys = logFields.Properties().Select(p => p.Name).ToList();
                                foreach (var key in keys) {
                                    logFields[key] = !logFields.Value<bool>(key);
                                }
                            }

                            Assert.True(client.Patch(Utils.Self(feature), JsonConvert.SerializeObject(feature), out result));

                            uFeature = JsonConvert.DeserializeObject<JObject>(result);

                            Assert.True(Utils.JEquals<bool>(feature, uFeature, "log_per_site"));
                            Assert.True(Utils.JEquals<bool>(feature, uFeature, "enabled"));
                            Assert.True(Utils.JEquals<string>(feature, uFeature, "log_file_encoding"));

                            Assert.True(Utils.JEquals<string>(feature, uFeature, "rollover.period"));
                            Assert.True(Utils.JEquals<long>(feature, uFeature, "rollover.truncate_size"));
                            Assert.True(Utils.JEquals<bool>(feature, uFeature, "rollover.use_local_time"));

                            Assert.True(JToken.DeepEquals(feature["log_target"], uFeature["log_target"]));
                            Assert.True(JToken.DeepEquals(feature["log_fields"], uFeature["log_fields"]));

                            feature = uFeature;

                        }
                        finally {
                            Assert.True(client.Patch(Utils.Self(cachedSpecific), JsonConvert.SerializeObject(cachedSpecific), out result));
                        }
                    }

                }
                finally {
                    Assert.True(client.Patch(Utils.Self(cachedFeature), JsonConvert.SerializeObject(cachedFeature), out result));
                }
            }
        }

        public static JObject GetLoggingFeature(HttpClient client, string siteName, string path)
        {
            if (path != null)
            {
                if (!path.StartsWith("/"))
                {
                    throw new ArgumentException("path");
                }
            }

            string content;
            if (!client.Get(LOGGING_URL + "?scope=" + siteName + path, out content))
            {
                return null;
            }

            return Utils.ToJ(content);
        }

        private class LogFormat
        {
            public bool IsServerLevel { get; set; }
            public string Name { get; set; }
        }
    }
}
