// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Tests
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using Web.Administration;
    using Xunit;
    using Xunit.Abstractions;

    public class Compression
    {
        public const string TEST_SITE_NAME = "compression_test_site";
        public static readonly string COMPRESSION_URL = $"{Configuration.Instance().TEST_SERVER_URL}/api/webserver/http-response-compression";

        private ITestOutputHelper _output;

        public Compression(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ChangeAllProperties()
        {
            using(HttpClient client = ApiHttpClient.Create()) {

                // Web Server Scope
                JObject webServerFeature = GetCompressionFeature(client, null, null);
                SetCompressionOverrideMode(client, webServerFeature, OverrideMode.Allow);

                ChangeAndRestoreProps(client, webServerFeature);

                // Site Scope
                Sites.EnsureNoSite(client, TEST_SITE_NAME);
                JObject site = Sites.CreateSite(_output, client, TEST_SITE_NAME, 53010, Sites.TEST_SITE_PATH);
                JObject siteFeature = GetCompressionFeature(client, site.Value<string>("name"), null);
                SetCompressionOverrideMode(client, siteFeature, OverrideMode.Allow);

                ChangeAndRestoreProps(client, siteFeature);

                // Application Scope
                JObject app = Applications.CreateApplication(client, "test_app", Path.Combine(Sites.TEST_SITE_PATH, "test_application"), site);
                JObject appFeature = GetCompressionFeature(client, site.Value<string>("name"), app.Value<string>("path"));
                Assert.NotNull(appFeature);
                SetCompressionOverrideMode(client, appFeature, OverrideMode.Allow);

                ChangeAndRestoreProps(client, appFeature);

                // Vdir Scope
                JObject vdir = VirtualDirectories.CreateVdir(client, "test_vdir", Path.Combine(Sites.TEST_SITE_PATH, "test_vdir"), site, true);
                JObject vdirFeature = GetCompressionFeature(client, site.Value<string>("name"), vdir.Value<string>("path"));
                Assert.NotNull(vdirFeature);
                SetCompressionOverrideMode(client, vdirFeature, OverrideMode.Allow);

                ChangeAndRestoreProps(client, vdirFeature);

                // Directory Scope
                var dirName = "test_directory";
                var dirPath = Path.Combine(Sites.TEST_SITE_PATH, dirName);
                if (!Directory.Exists(dirPath)) {
                    Directory.CreateDirectory(dirPath);
                }

                JObject directoryFeature = GetCompressionFeature(client, site.Value<string>("name"), $"/{dirName}");
                Assert.NotNull(directoryFeature);
                SetCompressionOverrideMode(client, directoryFeature, OverrideMode.Allow);

                ChangeAndRestoreProps(client, directoryFeature);

                Sites.DeleteSite(client, Utils.Self(site));
            }
        }

        private static void ChangeAndRestoreProps(HttpClient client, JObject feature)
        {
            feature.Remove("metadata");
            bool isWebServer = string.IsNullOrEmpty(feature.Value<string>("scope"));

            if (isWebServer) {
                feature["directory"] = Sites.TEST_SITE_PATH;
                feature["do_disk_space_limitting"] = !feature.Value<bool>("do_disk_space_limitting");
                feature["max_disk_space_usage"] = feature.Value<long>("max_disk_space_usage") + 1;
                feature["min_file_size"] = feature.Value<long>("min_file_size") + 1;
            }
            else {
                feature.Remove("directory");
                feature.Remove("do_disk_space_limitting");
                feature.Remove("max_disk_space_usage");
                feature.Remove("min_file_size");
            }

            JObject cachedFeature = new JObject(feature);

            feature["do_dynamic_compression"] = !feature.Value<bool>("do_dynamic_compression");
            feature["do_static_compression"] = !feature.Value<bool>("do_static_compression");

            string result;
            Assert.True(client.Patch(Utils.Self(feature), JsonConvert.SerializeObject(feature), out result));

            JObject newFeature = JsonConvert.DeserializeObject<JObject>(result);

            if (isWebServer) {
                Assert.True(Utils.JEquals<string>(feature, newFeature, "directory"));
                Assert.True(Utils.JEquals<bool>(feature, newFeature, "do_disk_space_limitting"));
                Assert.True(Utils.JEquals<string>(feature, newFeature, "max_disk_space_usage"));
                Assert.True(Utils.JEquals<string>(feature, newFeature, "min_file_size"));
            }

            Assert.True(Utils.JEquals<bool>(feature, newFeature, "do_dynamic_compression"));
            Assert.True(Utils.JEquals<bool>(feature, newFeature, "do_static_compression"));
            
            Assert.True(client.Patch(Utils.Self(feature), JsonConvert.SerializeObject(cachedFeature), out result));
        }

        public static JObject GetCompressionFeature(HttpClient client, string siteName, string path)
        {
            if (path != null) {
                if (!path.StartsWith("/")) {
                    throw new ArgumentException("path");
                }
            }

            string content;
            if (!client.Get(COMPRESSION_URL + "?scope=" + siteName + path, out content)) {
                return null;
            }

            var reference = Utils.ToJ(content);
            return Utils.FollowLink(client, reference, "self");
        }



        private static void SetCompressionOverrideMode(HttpClient client, JObject feature, OverrideMode overrideMode)
        {
            JObject ret = null;
            var delegation = client.Get($"{Delegation.DelegationSectionsUri}?scope={feature.Value<string>("scope")}");
            var sections = delegation.Value<JArray>("sections").ToObject<IEnumerable<JObject>>();

            foreach (var sec in sections) {
                if (sec.Value<string>("name").Equals("system.webServer/urlCompression")) {

                    var o = JObject.FromObject(new
                    {
                        override_mode = overrideMode
                    });


                    ret = client.Patch(Utils.Self(sec), o);
                    break;
                }
            }

            Assert.NotNull(ret);
        }
    }
}
