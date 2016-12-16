// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Tests
{
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using Xunit;

    public class Handlers
    {
        public const string TEST_SITE_NAME = "handlers_test_site";
        public static readonly string HANDLERS_URL = $"{Configuration.TEST_SERVER_URL}/api/webserver/http-handlers";

        private const string allowedAccessProperty = "allowed_access";
        private const string removePreventionProperty = "remote_access_prevention";

        [Fact]
        public void ChangeAllFeatureProperties()
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                var webserverFeature = Utils.GetFeature(client, HANDLERS_URL, null, null);
                Assert.NotNull(webserverFeature);

                AllowOverride(client, webserverFeature);
                TestScopedFeature(client, webserverFeature);
                
                Sites.EnsureNoSite(client, TEST_SITE_NAME);
                var site = Sites.CreateSite(client, TEST_SITE_NAME, Utils.GetAvailablePort(), Sites.TEST_SITE_PATH);
                Assert.NotNull(site);
                try {
                    var siteFeature = Utils.GetFeature(client, HANDLERS_URL, site.Value<string>("name"), "/");
                    Assert.NotNull(siteFeature);

                    AllowOverride(client, siteFeature);
                    TestScopedFeature(client, siteFeature);

                    client.Delete(Utils.Self(siteFeature));
                }
                finally {
                    Sites.EnsureNoSite(client, site.Value<string>("name"));
                }
            }
        }

        [Fact]
        public void CreateChangeRemoveHandler()
        {
            var handler = JObject.FromObject(new {
                name = "test_handler",
                path = "*.test",
                verbs = "*",
                type = "Microsoft.IIS.Administration.Test.Type",
                modules = "ManagedPipelineHandler",
                script_processor = "",
                resource_type = "unspecified",
                require_access = "script",
                allow_path_info = false,
                precondition = "integratedMode"
            });

            using (var client = ApiHttpClient.Create()) {
                var webserverFeature = Utils.GetFeature(client, HANDLERS_URL, null, null);
                Assert.NotNull(webserverFeature);

                var h = GetHandler(client, webserverFeature, handler.Value<string>("name"));
                if (h != null) {
                    Assert.True(client.Delete(Utils.Self(h)));
                }

                JObject createdHandler, patchedHandler = null;
                createdHandler = CreateHandlerForFeature(client, webserverFeature, handler);
                Assert.NotNull(createdHandler);

                try {
                    AssertHandlersEquals(handler, createdHandler);

                    string patchName = handler.Value<string>("name") + "2";
                    h = GetHandler(client, webserverFeature, patchName);
                    if (h != null) {
                        Assert.True(client.Delete(Utils.Self(h)));
                    }

                    createdHandler["name"] = patchName;
                    createdHandler["path"] = createdHandler.Value<string>("path") + "2";
                    createdHandler["verbs"] = "GET";
                    createdHandler["type"] = createdHandler.Value<string>("type") + "2";
                    createdHandler["modules"] = "";
                    createdHandler["script_processor"] = "some.dll";
                    createdHandler["resource_type"] = "file";
                    createdHandler["require_access"] = "write";
                    createdHandler["allow_path_info"] = !createdHandler.Value<bool>("allow_path_info");
                    createdHandler["precondition"] = "";

                    patchedHandler = client.Patch(Utils.Self(createdHandler), createdHandler);
                    Assert.NotNull(patchedHandler);

                    AssertHandlersEquals(createdHandler, patchedHandler);

                    Assert.True(client.Delete(Utils.Self(patchedHandler)));
                }
                finally {
                    if (createdHandler != null) {
                        client.Delete(Utils.Self(createdHandler));
                    }
                    if (patchedHandler != null) {
                        client.Delete(Utils.Self(patchedHandler));
                    }
                }
            }
        }

        private JObject CreateHandlerForFeature(HttpClient client, JObject feature, JObject handler)
        {
            handler.Add("handler", feature);
            return client.Post(Utils.GetLink(feature, "entries"), handler);
        }

        private JObject GetHandler(HttpClient client, JObject feature, string handlerName)
        {
            var handlers = Utils.FollowLink(client, feature, "entries")["entries"].ToObject<IEnumerable<JObject>>();
            return handlers.FirstOrDefault(h => h.Value<string>("name").Equals(handlerName, StringComparison.OrdinalIgnoreCase));
        }

        private void TestScopedFeature(HttpClient client, JObject feature)
        {
            var original = (JObject)feature.DeepClone();
            string result = null;

            try {
                ChangeAllFeatureProperties(feature);
                Assert.True(client.Patch(Utils.Self(feature), feature, out result));
                JObject patchedFeature = JObject.Parse(result);
                AssertFeaturesEqual(feature, patchedFeature);
                feature = patchedFeature;

                ChangeSomeFeatureProperties(feature);
                Assert.True(client.Patch(Utils.Self(feature), feature, out result));
                patchedFeature = JObject.Parse(result);
                AssertFeaturesEqual(feature, patchedFeature);
            }
            finally {
                client.Patch(Utils.Self(original), feature, out result);
            }
        }

        private void ChangeAllFeatureProperties(JObject handlers)
        {
            handlers[allowedAccessProperty]["read"] = !handlers[allowedAccessProperty].Value<bool>("read");
            handlers[allowedAccessProperty]["write"] = !handlers[allowedAccessProperty].Value<bool>("write");
            handlers[allowedAccessProperty]["execute"] = !handlers[allowedAccessProperty].Value<bool>("execute");
            handlers[allowedAccessProperty]["source"] = !handlers[allowedAccessProperty].Value<bool>("source");
            handlers[allowedAccessProperty]["script"] = !handlers[allowedAccessProperty].Value<bool>("script");

            handlers[removePreventionProperty]["write"] = !handlers[removePreventionProperty].Value<bool>("write");
            handlers[removePreventionProperty]["read"] = !handlers[removePreventionProperty].Value<bool>("read");
            handlers[removePreventionProperty]["execute"] = !handlers[removePreventionProperty].Value<bool>("execute");
            handlers[removePreventionProperty]["script"] = !handlers[removePreventionProperty].Value<bool>("script");
        }

        private void ChangeSomeFeatureProperties(JObject handlers)
        {
            handlers[allowedAccessProperty]["write"] = !handlers[allowedAccessProperty].Value<bool>("write");
            handlers[removePreventionProperty]["read"] = !handlers[removePreventionProperty].Value<bool>("read");
            handlers[removePreventionProperty]["script"] = !handlers[removePreventionProperty].Value<bool>("script");
        }

        private void AssertFeaturesEqual(JObject a, JObject b)
        {
            Assert.True(Utils.JEquals<bool>(a, b, allowedAccessProperty + ".read"));
            Assert.True(Utils.JEquals<bool>(a, b, allowedAccessProperty + ".write"));
            Assert.True(Utils.JEquals<bool>(a, b, allowedAccessProperty + ".execute"));
            Assert.True(Utils.JEquals<bool>(a, b, allowedAccessProperty + ".source"));
            Assert.True(Utils.JEquals<bool>(a, b, allowedAccessProperty + ".script"));

            Assert.True(Utils.JEquals<bool>(a, b, removePreventionProperty + ".write"));
            Assert.True(Utils.JEquals<bool>(a, b, removePreventionProperty + ".read"));
            Assert.True(Utils.JEquals<bool>(a, b, removePreventionProperty + ".execute"));
            Assert.True(Utils.JEquals<bool>(a, b, removePreventionProperty + ".script"));
        }

        private void AssertHandlersEquals(JObject a, JObject b)
        {
            var handler = new
            {
                name = "test_handler",
                path = "*.test",
                verbs = "*",
                type = "Microsoft.IIS.Administration.Test.Type",
                modules = "ManagedPipelineHandler",
                script_processor = "",
                resource_type = "unspecified",
                require_access = "script",
                allow_path_info = false,
                precondition = "integratedMode"
            };

            Assert.True(Utils.JEquals<string>(a, b, "name"));
            Assert.True(Utils.JEquals<string>(a, b, "path"));
            Assert.True(Utils.JEquals<string>(a, b, "verbs"));
            Assert.True(Utils.JEquals<string>(a, b, "type"));
            Assert.True(Utils.JEquals<string>(a, b, "modules"));
            Assert.True(Utils.JEquals<string>(a, b, "script_processor"));
            Assert.True(Utils.JEquals<string>(a, b, "resource_type"));
            Assert.True(Utils.JEquals<string>(a, b, "require_access"));
            Assert.True(Utils.JEquals<bool>(a, b, "allow_path_info"));
            Assert.True(Utils.JEquals<string>(a, b, "precondition"));
        }

        private void AllowOverride(HttpClient client, JObject feature)
        {
            string result = null;
            feature["metadata"]["override_mode"] = "allow";
            Assert.True(client.Patch(Utils.Self(feature), feature, out result));
        }
    }
}

