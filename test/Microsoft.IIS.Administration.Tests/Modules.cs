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
    using Xunit;

    public class Modules
    {
        public static readonly string GLOBAL_MODULES_URL = $"{Configuration.Instance().TEST_SERVER_URL}/api/webserver/global-modules";
        public static readonly string MODULES_URL = $"{Configuration.Instance().TEST_SERVER_URL}/api/webserver/http-modules";

        [Fact]
        public void AddRemoveManagedModuleEntry()
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                JObject feature = GetModulesFeature(client, null, null);

                string modulesLink = Utils.GetLink(feature, "entries");

                var testManagedModule = new {
                    name = "test_managed_module",
                    type = "test.managed.module",
                    modules = feature
                };

                string result;
                Assert.True(client.Post(modulesLink, JsonConvert.SerializeObject(testManagedModule), out result));

                JObject managedModule = JsonConvert.DeserializeObject<JObject>(result);

                Assert.True(client.Get(Utils.Self(managedModule), out result));
                Assert.True(client.Delete(Utils.Self(managedModule)));

            }
        }

        [Fact]
        public void AddRemoveNativedModuleEntry()
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                string name = "test_native_module";

                JObject modulesFeature = GetModulesFeature(client, null, null);
                DeleteModule(client, modulesFeature, name);

                var globalModulePayload = new {
                    name = name,
                    // Use a module that exists with IIS installations because the dll must exist to create Global Module
                    image = "%windir%\\System32\\inetsrv\\cachuri.dll",
                };

                string result;
                Assert.True(client.Post(GLOBAL_MODULES_URL, JsonConvert.SerializeObject(globalModulePayload), out result));

                JObject globalModule = JsonConvert.DeserializeObject<JObject>(result);

                modulesFeature = GetModulesFeature(client, null, null);

                string modulesLink = Utils.GetLink(modulesFeature, "entries");

                var nativeModulePayload = new {
                    name = globalModule.Value<string>("name"),
                    // Type must be empty for native module
                    type = "",
                    modules = modulesFeature
                };
                
                

                Assert.True(client.Post(modulesLink, JsonConvert.SerializeObject(nativeModulePayload), out result));

                JObject nativeModule = JsonConvert.DeserializeObject<JObject>(result);

                // Make sure we can successfully retrieve the new modules
                Assert.True(client.Get(Utils.Self(nativeModule), out result));
                Assert.True(client.Get(Utils.Self(globalModule), out result));

                // Delete the native module in module entries
                Assert.True(client.Delete(Utils.Self(nativeModule)));

                // Delete the global module
                Assert.True(client.Delete(Utils.Self(globalModule)));

            }
        }



        public static JObject GetGlobalModulesFeature(HttpClient client)
        {

            string content;
            if (!client.Get(GLOBAL_MODULES_URL, out content)) {
                return null;
            }

            return Utils.ToJ(content);
        }

        public static JObject GetModulesFeature(HttpClient client, string siteName, string path)
        {
            if (path != null) {
                if (!path.StartsWith("/")) {
                    throw new ArgumentException("path");
                }
            }

            string content;
            if (!client.Get(MODULES_URL + "?scope=" + siteName + path, out content)) {
                return null;
            }

            return Utils.ToJ(content);
        }

        public static void DeleteModule(HttpClient client, JObject modulesFeature, string name)
        {
            if(modulesFeature != null) { 
                string modulesLink = Utils.GetLink(modulesFeature, "entries");

                string result;
                if(!client.Get(modulesLink, out result))
                {
                    throw new Exception();
                }

                JObject modulesRep = JsonConvert.DeserializeObject<JObject>(result);
                var entries = modulesRep.Value<JArray>("entries").ToObject<IEnumerable<JObject>>();

                JObject targetModule = entries.FirstOrDefault(e => e.Value<string>("name").Equals(name, StringComparison.OrdinalIgnoreCase));

                if(targetModule != null && !client.Delete(Utils.Self(targetModule)))
                {
                    throw new Exception();
                }
            }
            
            var globalModules = GetGlobalModulesFeature(client).Value<JArray>("global_modules").ToObject<IEnumerable<JObject>>(); ;

            JObject targetGlobalModule = globalModules.FirstOrDefault(e => e.Value<string>("name").Equals(name, StringComparison.OrdinalIgnoreCase));

            if (targetGlobalModule != null && !client.Delete(Utils.Self(targetGlobalModule)))
            {
                throw new Exception();
            }
        }
    }
}
