// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Tests
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Linq;
    using System.Net.Http;
    using Xunit;

    public class DefaultDocument
    {
        private const string TEST_SITE_NAME = "def_doc_test_site";
        
        public static readonly string DEFAULT_DOCUMENT_URL = $"{Globals.TEST_SERVER}:{Globals.TEST_PORT}/api/webserver/default-documents";

        [Fact]
        public void ScopeTest()
        {
            using (HttpClient client = ApiHttpClient.Create($"{Globals.TEST_SERVER}:{Globals.TEST_PORT}")) {

                Sites.EnsureNoSite(client, TEST_SITE_NAME);
                JObject site = Sites.CreateSite(client, TEST_SITE_NAME, 50311, @"c:\sites\test_site");

                JObject serverDoc = GetDefaultDocumentFeature(client, null, null);
                JObject siteDoc = GetDefaultDocumentFeature(client, site.Value<string>("name"), null);

                bool prevServerState = serverDoc.Value<bool>("enabled");

                bool testServerState = true;
                bool testSiteState = false;

                // Server level configuration change
                Assert.True(SetEnabled(client, serverDoc, testServerState, out serverDoc));

                // Site level configuration change
                Assert.True(SetEnabled(client, siteDoc, testSiteState, out siteDoc));

                // Make sure site change didn't affect server level
                serverDoc = GetDefaultDocumentFeature(client, null, null);
                Assert.True(serverDoc.Value<bool>("enabled") == testServerState);

                Assert.True(Sites.DeleteSite(client, Utils.Self(site)));
                Assert.True(SetEnabled(client, serverDoc, prevServerState, out serverDoc));
            }
        }

        [Fact]
        public void CreateFileTest()
        {
            using (HttpClient client = ApiHttpClient.Create($"{Globals.TEST_SERVER}:{Globals.TEST_PORT}")) {
                string fileName = "test_file";

                // Web Server Scope
                JObject webServerFeature = GetDefaultDocumentFeature(client, null, null);
                Assert.NotNull(webServerFeature);

                CreateAndRemoveFile(client, webServerFeature, fileName);

                // Site Scope
                Sites.EnsureNoSite(client, TEST_SITE_NAME);
                JObject site = Sites.CreateSite(client, TEST_SITE_NAME, 50311, @"c:\sites\test_site");
                JObject siteFeature = GetDefaultDocumentFeature(client, site.Value<string>("name"), null);
                Assert.NotNull(siteFeature);

                CreateAndRemoveFile(client, siteFeature, fileName);

                // Application Scope
                JObject app = Applications.CreateApplication(client, "test_app", @"c:\sites\test_site\test_application", site);
                JObject appFeature = GetDefaultDocumentFeature(client, site.Value<string>("name"), app.Value<string>("path"));
                Assert.NotNull(appFeature);

                CreateAndRemoveFile(client, appFeature, fileName);

                // Vdir Scope
                JObject vdir = VirtualDirectories.CreateVdir(client, "test_vdir", @"c:\sites\test_site\test_vdir", site);
                JObject vdirFeature = GetDefaultDocumentFeature(client, site.Value<string>("name"), vdir.Value<string>("path"));
                Assert.NotNull(vdirFeature);

                CreateAndRemoveFile(client, vdirFeature, fileName);

                // Directory Scope
                JObject directoryFeature = GetDefaultDocumentFeature(client, site.Value<string>("name"), "/test_directory");
                Assert.NotNull(directoryFeature);

                CreateAndRemoveFile(client, directoryFeature, fileName);
            }
        }

        private static void CreateAndRemoveFile(HttpClient client, JObject feature, string fileName)
        {
            JObject file = GetFile(client, feature, fileName);

            if (file != null) {
                Assert.True(DeleteFile(client, file));
            }

            file = CreateFile(client, feature, fileName);
            Assert.NotNull(file);
            Assert.True(DeleteFile(client, file));
        }


        public static bool SetEnabled(HttpClient client, JObject docFeature, bool enabled, out JObject document)
        {
            document = null;
            string body = "{ \"enabled\" : \"" + enabled.ToString() + "\"  }";

            string docContent;
            
            if(!client.Patch(Utils.Self(docFeature), body, out docContent)) {
                return false;
            }

            document = Utils.ToJ(docContent);

            bool? docEnabled = document.Value<bool?>("enabled");

            if(docEnabled  == null || docEnabled.Value != enabled) {
                return false;
            }

            return true;
        }

        public static JArray GetFiles(HttpClient client, JObject docFeature)
        {
            docFeature = Utils.FollowLink(client, docFeature, "entries");

            return docFeature.Value<JArray>("entries");
        }

        public static JObject CreateFile(HttpClient client, JObject docFeature, string fileName)
        {
            string featureUuid = docFeature.Value<string>("id");

            if(featureUuid == null) {
                throw new ArgumentException("docFeature");
            }

            string filesLink = $"{ Globals.TEST_SERVER }:{ Globals.TEST_PORT }{ docFeature["_links"]["entries"].Value<string>("href") }";

            dynamic feature = new JObject();
            feature.id = featureUuid;

            dynamic body = new JObject();
            body.name = fileName;
            body.default_document = feature;

            string str = body.ToString();

            string result;
            if(client.Post(filesLink, str, out result)) {
                return JsonConvert.DeserializeObject<JObject>(result);
            }

            return null;
        }

        public static JObject GetFile(HttpClient client, JObject docFeature, string fileName)
        {
            JArray files = GetFiles(client, docFeature);

            JObject file = files.FirstOrDefault(f => f.Value<string>("name").Equals(fileName, StringComparison.OrdinalIgnoreCase)) as JObject;

            if(file != null) {
                file = Utils.FollowLink(client, file, "self");
            }

            return file;
        }

        public static bool DeleteFile(HttpClient client, JObject file)
        {
            return client.Delete(Utils.Self(file));
        }

        public static JObject GetDefaultDocumentFeature(HttpClient client, string siteName, string path)
        {
            if(path != null) {
                if (!path.StartsWith("/")) {
                    throw new ArgumentException("path");
                }
            }

            string content;
            if(!client.Get(DEFAULT_DOCUMENT_URL + "?scope=" + siteName + path, out content)) {
                return null;
            }

            return Utils.ToJ(content);
        }
    }
}
