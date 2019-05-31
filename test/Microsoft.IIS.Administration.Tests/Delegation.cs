// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Tests
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using Xunit;
    using Xunit.Abstractions;

    public class Delegation
    {
        public static readonly string DelegationSectionsUri = $"{Configuration.Instance().TEST_SERVER_URL}/api/webserver/feature-delegation";

        private const string TEST_SITE_NAME = "delegation_test_site";

        private ITestOutputHelper _output;

        public Delegation(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void SectionLocked()
        {
            string webServerPath = "/api/webserver";

            List<DelegatableFeature> features = new List<DelegatableFeature>() {
                new DelegatableFeature {
                    Name = "default_documents",
                    Path = webServerPath + "/default-documents",
                    EditablePropertyName = "enabled",
                    EditableType = typeof(bool)
                },
                new DelegatableFeature {
                    Name = "windows_authentication",
                    Path = webServerPath + "/authentication/windows-authentication",
                    EditablePropertyName = "enabled",
                    EditableType = typeof(bool)
                },
                new DelegatableFeature {
                    Name = "anonymous_authentication",
                    Path = webServerPath + "/authentication/anonymous-authentication",
                    EditablePropertyName = "enabled",
                    EditableType = typeof(bool)
                },
                new DelegatableFeature {
                    Name = "basic_authentication",
                    Path = webServerPath + "/authentication/basic-authentication",
                    EditablePropertyName = "enabled",
                    EditableType = typeof(bool)
                },
                new DelegatableFeature {
                    Name = "digest_authentication",
                    Path = webServerPath + "/authentication/digest-authentication",
                    EditablePropertyName = "enabled",
                    EditableType = typeof(bool)
                },
                new DelegatableFeature {
                    Name = "authorization",
                    Path = webServerPath + "/authorization",
                    EditablePropertyName = "bypass_login_pages",
                    EditableType = typeof(bool)
                },
                new DelegatableFeature {
                    Name = "directory_browsing",
                    Path = webServerPath + "/directory-browsing",
                    EditablePropertyName = "enabled",
                    EditableType = typeof(bool)
                },
                new DelegatableFeature {
                    Name = "http_response_headers",
                    Path = webServerPath + "/http-response-headers",
                    EditablePropertyName = "allow_keep_alive",
                    EditableType = typeof(bool)
                },
                new DelegatableFeature {
                    Name = "ip_restrictions",
                    Path = webServerPath + "/ip-restrictions",
                    EditablePropertyName = "allow_unlisted",
                    EditableType = typeof(bool)
                },
                new DelegatableFeature {
                    Name = "logging",
                    Path = webServerPath + "/logging",
                    EditablePropertyName = "dont_log",
                    EditableType = typeof(bool)
                },
                new DelegatableFeature {
                    Name = "modules",
                    Path = webServerPath + "/http-modules",
                    EditablePropertyName = "run_all_managed_modules_for_all_requests",
                    EditableType = typeof(bool)
                },
                new DelegatableFeature {
                    Name = "request_filtering",
                    Path = webServerPath + "/http-request-filtering",
                    EditablePropertyName = "allow_unlisted_file_extensions",
                    EditableType = typeof(bool)
                },
                new DelegatableFeature {
                    Name = "static_content",
                    Path = webServerPath + "/static-content",
                    EditablePropertyName = "enable_doc_footer",
                    EditableType = typeof(bool)
                }
            };

            using (HttpClient client = ApiHttpClient.Create()) {

                Sites.EnsureNoSite(client, TEST_SITE_NAME);
                JObject site = Sites.CreateSite(_output, client, TEST_SITE_NAME, 50310, Sites.TEST_SITE_PATH);

                foreach (DelegatableFeature f in features) {

                    // Get web server scope feature
                    JObject feature = GetScopedFeature(client, f, null, null);

                    // Change delegation state of the feature to allow
                    feature["metadata"]["override_mode"] = "allow";

                    string result;

                    // Send delegation allow patch to server
                    Assert.True(client.Patch(Utils.Self(feature), JsonConvert.SerializeObject(feature), out result));

                    // Get site level feature
                    feature = GetScopedFeature(client, f, TEST_SITE_NAME, null);

                    // Edit the feature at site level
                    EditFeature(feature, f);
                    Assert.True(client.Patch(Utils.Self(feature), JsonConvert.SerializeObject(feature), out result));

                    feature = JsonConvert.DeserializeObject<JObject>(result);

                    // Make sure we were able to edit the feature at the site level to set up the locked error
                    Assert.True(feature["metadata"].Value<bool>("is_local") == true);

                    // Get web server scope feature
                    feature = GetScopedFeature(client, f, null, null);
                    
                    feature["metadata"]["override_mode"] = "deny";

                    // Deny override
                    Assert.True(client.Patch(Utils.Self(feature), JsonConvert.SerializeObject(feature), out result));

                    // Try to get the feature at site level, this should result in a feature locked error because the override mode at server
                    // level is deny
                    var response = client.GetAsync($"{Configuration.Instance().TEST_SERVER_URL}{f.Path}?scope={TEST_SITE_NAME}").Result;

                    // Check for proper status code for feature locked error
                    Assert.True(response.StatusCode == System.Net.HttpStatusCode.Forbidden);

                    // Check for proper error title for feature locked
                    JObject responseBody = JsonConvert.DeserializeObject<JObject>(response.Content.ReadAsStringAsync().Result);
                    Assert.True(responseBody.Value<string>("title") == "Object is locked");

                    // Get web server scope feature
                    feature = GetScopedFeature(client, f, null, null);

                    // Change delegation state of the feature to allow
                    feature["metadata"]["override_mode"] = "allow";

                    // Send delegation allow patch to server
                    Assert.True(client.Patch(Utils.Self(feature), JsonConvert.SerializeObject(feature), out result));

                    // Get site level feature
                    feature = GetScopedFeature(client, f, TEST_SITE_NAME, null);

                    // Make sure we could get the feature fine, now that the section has been unlocked at server level
                    Assert.NotNull(feature);

                    // Cleanup the feature at site level by removing its local configuration
                    Assert.True(client.Delete(Utils.Self(feature)));
                }

                Sites.DeleteSite(client, Utils.Self(site));
            }
        }


        [Fact]
        public void ManipulateSections()
        {

            using (HttpClient client = ApiHttpClient.Create()) {

                string result;

                Assert.True(client.Get(DelegationSectionsUri, out result));

                JObject delegation = JsonConvert.DeserializeObject<JObject>(result);

                JArray sections = delegation.Value<JArray>("sections");

                foreach(JObject sectionRef in sections) {

                    ManipulateSection(client, sectionRef);
                }
            }
        }

        private void ManipulateSection(HttpClient client, JObject section)
        {
            string result;

            // Ensure we are viewing the full section
            Assert.True(client.Get(Utils.Self(section), out result));
            section = JsonConvert.DeserializeObject<JObject>(result);

            section["override_mode"] = "allow";

            Assert.True(client.Patch(Utils.Self(section), JsonConvert.SerializeObject(section), out result));
            section = JsonConvert.DeserializeObject<JObject>(result);

            Assert.Equal(section.Value<string>("override_mode"), "allow");

            section["override_mode"] = "deny";

            Assert.True(client.Patch(Utils.Self(section), JsonConvert.SerializeObject(section), out result));
            section = JsonConvert.DeserializeObject<JObject>(result);

            Assert.Equal(section.Value<string>("override_mode"), "deny");

            section["override_mode"] = "inherit";

            Assert.True(client.Patch(Utils.Self(section), JsonConvert.SerializeObject(section), out result));
            section = JsonConvert.DeserializeObject<JObject>(result);

            Assert.Equal(section.Value<string>("override_mode"), "inherit");
        }

        private static void EditFeature(JObject featureRep, DelegatableFeature feature)
        {
            if(feature.EditableType == typeof(bool)) {
                featureRep[feature.EditablePropertyName] = !featureRep.Value<bool>(feature.EditablePropertyName);
            }
        }

        private static JObject GetScopedFeature(HttpClient client, DelegatableFeature feature, string siteName, string path)
        {
            if (path != null) {
                if (!path.StartsWith("/")) {
                    throw new ArgumentException("path");
                }
            }

            string content;
            if (!client.Get($"{Configuration.Instance().TEST_SERVER_URL}" + feature.Path + "?scope=" + siteName + path, out content)) {
                return null;
            }

            return Utils.ToJ(content);
        }

        private class DelegatableFeature
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public string EditablePropertyName { get; set; }
            public Type EditableType { get; set; }
        }
    }
}
