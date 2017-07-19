// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Tests
{
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Xunit;

    public class UrlRewrite
    {
        private const string TEST_SITE_NAME = "Rewrite Test Site";
        private static readonly string REWRITE_URL = $"{Configuration.TEST_SERVER_URL}/api/webserver/url-rewrite";

        [Fact]
        public async Task CreateAndUpdateInboundRule()
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                await EnsureEnabled(client);

                Sites.EnsureNoSite(client, TEST_SITE_NAME);
                var site = Sites.CreateSite(client, TEST_SITE_NAME, Utils.GetAvailablePort(), Sites.TEST_SITE_PATH);
                Assert.NotNull(site);

                try {
                    JObject siteFeature = Utils.GetFeature(client, REWRITE_URL, site.Value<string>("name"), "/");
                    Assert.NotNull(siteFeature);

                    const string testRuleName = "RewriteTestRule";
                    const string updatedTestRuleName = testRuleName + "2";
                    const string testServerVariable1 = "abc", testServerVariable2 = "def";

                    JObject inboundRulesSection = Utils.FollowLink(client, siteFeature, "inbound_rules");
                    string inboundRulesLink = Utils.GetLink(inboundRulesSection, "rules");

                    IEnumerable<JObject> rules = client.Get(inboundRulesLink)["rules"].ToObject<IEnumerable<JObject>>();
                    foreach (var r in rules) {
                        if (r.Value<string>("name").Equals(testRuleName, StringComparison.OrdinalIgnoreCase) ||
                            r.Value<string>("name").Equals(updatedTestRuleName, StringComparison.OrdinalIgnoreCase)) {
                            Assert.True(client.Delete(Utils.Self(r)));
                        }
                    }

                    //
                    // Ensure server variables allowed
                    await AddAllowedServerVariable(testServerVariable1);
                    await AddAllowedServerVariable(testServerVariable2);

                    JObject rule = JObject.FromObject(new {
                        name = testRuleName,
                        pattern = "^([^/]+)/([^/]+)/?$",
                        pattern_syntax = "regular_expression",
                        ignore_case = true,
                        negate = true,
                        stop_processing = true,
                        action = new {
                            type = "rewrite",
                            url = "def.aspx?a={R:1}&c={R:2}",
                            append_query_string = true,
                            description = "A test rule",
                            reason = "Replace url"
                        },
                        server_variables = new object[] {
                            new {
                                name = testServerVariable1,
                                value = "def",
                                replace = true
                            }
                        },
                        conditions = new object[] {
                            new {
                                input = "{REQUEST_FILENAME}",
                                pattern = "",
                                negate = true,
                                ignore_case = true,
                                match_type = "isfile"
                            }
                        },
                        inbound_rules = inboundRulesSection
                    });

                    JObject result = client.Post(inboundRulesLink, rule);

                    Assert.NotNull(result);

                    AssertInboundRulesEqual(rule, result);

                    JObject update = JObject.FromObject(new {
                        name = updatedTestRuleName,
                        pattern = "abcdefg",
                        pattern_syntax = "wildcard",
                        ignore_case = false,
                        negate = false,
                        stop_processing = false,
                        action = new {
                            type = "redirect",
                            url = "def.aspx",
                            append_query_string = false,
                            description = "A test rule2",
                            reason = "Replace url2"
                        },
                        server_variables = new object[] {
                            new {
                                name = testServerVariable2,
                                value = "abc",
                                replace = false
                            }
                        },
                        conditions = new object[] {
                            new {
                                input = "{REQUEST_FILENAME}2",
                                pattern = "abc",
                                negate = false,
                                ignore_case = false,
                                match_type = "isdirectory"
                            }
                        }
                    });

                    JObject updatedResult = client.Patch(Utils.Self(result), update);

                    AssertInboundRulesEqual(updatedResult, update);

                    Assert.True(client.Delete(Utils.Self(updatedResult)));
                }
                finally {
                    Sites.EnsureNoSite(client, site.Value<string>("name"));
                }
            }
        }

        [Fact]
        public async Task CreateAndUpdateGlobalRule()
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                await EnsureEnabled(client);
                JObject webserverFeature = Utils.GetFeature(client, REWRITE_URL, "", null);
                Assert.NotNull(webserverFeature);

                const string testRuleName = "GlobalTestRule";
                string updatedTestRuleName = testRuleName + "2";

                JObject globalRulesSection = Utils.FollowLink(client, webserverFeature, "global_rules");
                string globalRulesLink = Utils.GetLink(globalRulesSection, "rules");

                IEnumerable<JObject> rules = client.Get(globalRulesLink)["rules"].ToObject<IEnumerable<JObject>>();
                foreach (var r in rules) {
                    if (r.Value<string>("name").Equals(testRuleName, StringComparison.OrdinalIgnoreCase) ||
                        r.Value<string>("name").Equals(updatedTestRuleName, StringComparison.OrdinalIgnoreCase)) {
                        Assert.True(client.Delete(Utils.Self(r)));
                    }
                }

                JObject rule = JObject.FromObject(new {
                    name = testRuleName,
                    pattern = "^([^/]+)/([^/]+)/?$",
                    pattern_syntax = "regular_expression",
                    ignore_case = true,
                    negate = true,
                    stop_processing = true,
                    action = new {
                        type = "rewrite",
                        url = "def.aspx?a={R:1}&c={R:2}",
                        append_query_string = true,
                        description = "A test rule",
                        reason = "Replace url"
                    },
                    server_variables = new object[] {
                            new {
                                name = "abc",
                                value = "def",
                                replace = true
                            }
                        },
                    conditions = new object[] {
                            new {
                                input = "{REQUEST_FILENAME}",
                                pattern = "",
                                negate = true,
                                ignore_case = true,
                                match_type = "pattern"
                            }
                        },
                    global_rules = globalRulesSection
                });

                JObject result = client.Post(globalRulesLink, rule);

                Assert.NotNull(result);

                AssertInboundRulesEqual(rule, result);

                JObject update = JObject.FromObject(new {
                    name = updatedTestRuleName,
                    pattern = "abcdefg",
                    pattern_syntax = "wildcard",
                    ignore_case = false,
                    negate = false,
                    stop_processing = false,
                    action = new {
                        type = "redirect",
                        url = "def.aspx",
                        append_query_string = false,
                        description = "A test rule2",
                        reason = "Replace url2"
                    },
                    server_variables = new object[] {
                            new {
                                name = "def",
                                value = "abc",
                                replace = false
                            }
                        },
                    conditions = new object[] {
                            new {
                                input = "{REQUEST_FILENAME}2",
                                pattern = "abc",
                                negate = false,
                                ignore_case = false,
                                match_type = "pattern"
                            }
                        }
                });

                JObject updatedResult = client.Patch(Utils.Self(result), update);

                AssertInboundRulesEqual(updatedResult, update);

                Assert.True(client.Delete(Utils.Self(updatedResult)));
            }
        }

        [Fact]
        public async Task UpdateAllowedServerVariables()
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                await EnsureEnabled(client);

                JObject webserverFeature = Utils.GetFeature(client, REWRITE_URL, "", null);
                Assert.NotNull(webserverFeature);

                JObject allowedServerVariablesResource = Utils.FollowLink(client, webserverFeature, "allowed_server_variables");

                string[] oldVariables = allowedServerVariablesResource["entries"].ToObject<string[]>();

                string[] variables = new string[] { "these", "are", "test", "variables" };

                allowedServerVariablesResource["entries"] = JToken.FromObject(variables);

                JObject result = client.Patch(Utils.Self(allowedServerVariablesResource), allowedServerVariablesResource);

                string[] resultVariables = result["entries"].ToObject<string[]>();

                Assert.Equal(variables, resultVariables);

                allowedServerVariablesResource["entries"] = JToken.FromObject(oldVariables);

                result = client.Patch(Utils.Self(allowedServerVariablesResource), allowedServerVariablesResource);

                resultVariables = result["entries"].ToObject<string[]>();

                Assert.Equal(oldVariables, resultVariables);
            }
        }

        [Fact]
        public async Task CreateAndUpdateRewriteMap()
        {
            const string testMapName = "TestRewriteMap";
            string updatedTestMapName = testMapName + "2";

            using (HttpClient client = ApiHttpClient.Create()) {

                await EnsureEnabled(client);

                JObject webserverFeature = Utils.GetFeature(client, REWRITE_URL, "", null);
                Assert.NotNull(webserverFeature);

                JObject rewriteMaps = Utils.FollowLink(client, webserverFeature, "rewrite_maps");
                string mapsLink = Utils.GetLink(rewriteMaps, "maps");

                foreach (var m in client.Get(mapsLink)["maps"].ToObject<IEnumerable<JObject>>()) {
                    if (m.Value<string>("name").Equals(testMapName, StringComparison.OrdinalIgnoreCase)
                            || m.Value<string>("name").Equals(updatedTestMapName, StringComparison.OrdinalIgnoreCase)) {
                        Assert.True(client.Delete(Utils.Self(m)));
                    }
                }

                JObject map = JObject.FromObject(new {
                    name = testMapName,
                    default_value = "def1",
                    ignore_case = false,
                    entries = new object[] {
                            new {
                                key = "abc",
                                value = "def"
                            },
                            new {
                                key = "zyx",
                                value = "wvu"
                            }
                        },
                    rewrite_maps = rewriteMaps
                });

                JObject result = client.Post(mapsLink, map);

                Assert.NotNull(result);

                AssertRewriteMapsEqual(map, result);

                JObject updateMap = JObject.FromObject(new {
                    name = updatedTestMapName,
                    default_value = "def2",
                    ignore_case = true,
                    entries = new object[] {
                            new {
                                key = "igloo",
                                value = "anonymous"
                            },
                            new {
                                key = "tea",
                                value = "coffee"
                            },
                            new {
                                key = "record",
                                value = "paper"
                            }
                        },
                    rewrite_maps = rewriteMaps
                });

                result = client.Patch(Utils.Self(result), updateMap);

                Assert.NotNull(result);

                AssertRewriteMapsEqual(updateMap, result);

                Assert.True(client.Delete(Utils.Self(result)));
            }
        }

        [Fact]
        public async Task CreateAndUpdateOutboundPrecondition()
        {
            const string testName = "TestPrecondition";
            string updatedName = testName + "2";

            using (HttpClient client = ApiHttpClient.Create()) {

                await EnsureEnabled(client);

                JObject webserverFeature = Utils.GetFeature(client, REWRITE_URL, "", null);
                Assert.NotNull(webserverFeature);

                JObject outboundRules = Utils.FollowLink(client, webserverFeature, "outbound_rules");
                string preConditionsLink = Utils.GetLink(outboundRules, "preconditions");

                foreach (var p in client.Get(preConditionsLink)["preconditions"].ToObject<IEnumerable<JObject>>()) {
                    if (p.Value<string>("name").Equals(testName, StringComparison.OrdinalIgnoreCase)
                            || p.Value<string>("name").Equals(updatedName, StringComparison.OrdinalIgnoreCase)) {
                        Assert.True(client.Delete(Utils.Self(p)));
                    }
                }

                JObject precondition = JObject.FromObject(new {
                    name = testName,
                    match = "all",
                    pattern_syntax = "regular_expression",
                    requirements = new object[] {
                            new {
                                input = "{RESPONSE_CONTENT_TYPE}",
                                pattern = "text/html",
                                negate = true,
                                ignore_case = true
                            },
                            new {
                                input = "{RESPONSE_CONTENT_LENGTH}",
                                pattern = "500",
                                negate = false,
                                ignore_case = false
                            }
                        },
                    outbound_rules = outboundRules
                });

                JObject result = client.Post(preConditionsLink, precondition);

                Assert.NotNull(result);

                AssertPreconditionsEqual(precondition, result);

                JObject updatePrecondition = JObject.FromObject(new {
                    name = updatedName,
                    match = "any",
                    pattern_syntax = "regular_expression",
                    requirements = new object[] {
                            new {
                                input = "{RESPONSE_SERVE}",
                                pattern = "a_server",
                                negate = false,
                                ignore_case = false
                            },
                            new {
                                input = "{RESPONSE_CONTENT_ENCODING}",
                                pattern = "gzip",
                                negate = true,
                                ignore_case = true
                            }
                        },
                });

                result = client.Patch(Utils.Self(result), updatePrecondition);

                Assert.NotNull(result);

                AssertPreconditionsEqual(updatePrecondition, result);

                Assert.True(client.Delete(Utils.Self(result)));
            }
        }

        [Fact]
        public async Task CreateAndUpdateOutboundCustomTags()
        {
            const string testName = "TestCustomTags";
            string updatedName = testName + "2";

            using (HttpClient client = ApiHttpClient.Create()) {

                await EnsureEnabled(client);

                JObject webserverFeature = Utils.GetFeature(client, REWRITE_URL, "", null);
                Assert.NotNull(webserverFeature);

                JObject outboundRules = Utils.FollowLink(client, webserverFeature, "outbound_rules");
                string customTagsLink = Utils.GetLink(outboundRules, "custom_tags");

                foreach (var p in client.Get(customTagsLink)["custom_tags"].ToObject<IEnumerable<JObject>>()) {
                    if (p.Value<string>("name").Equals(testName, StringComparison.OrdinalIgnoreCase)
                            || p.Value<string>("name").Equals(updatedName, StringComparison.OrdinalIgnoreCase)) {
                        Assert.True(client.Delete(Utils.Self(p)));
                    }
                }

                JObject customTags = JObject.FromObject(new {
                    name = testName,
                    tags = new object[] {
                            new {
                                name = "myHtmlTag",
                                attribute = "myHtmlAttribute"
                            },
                            new {
                                name = "anotherHtmlTag",
                                attribute = "anotherHtmlAttribute"
                            }
                        },
                    outbound_rules = outboundRules
                });

                JObject result = client.Post(customTagsLink, customTags);

                Assert.NotNull(result);

                AssertCustomTagsEqual(customTags, result);

                JObject updateCustomTags = JObject.FromObject(new {
                    name = updatedName,
                    tags = new object[] {
                            new {
                                name = "ATag",
                                attribute = "AnAttribute"
                            },
                            new {
                                name = "TestTag",
                                attribute = "TestAttribute"
                            }
                        },
                    outbound_rules = outboundRules
                });

                result = client.Patch(Utils.Self(result), updateCustomTags);

                Assert.NotNull(result);

                AssertCustomTagsEqual(updateCustomTags, result);

                Assert.True(client.Delete(Utils.Self(result)));
            }
        }

        [Fact]
        public async Task CreateAndUpdateOutboundRule()
        {
            const string testName = "TestCustomRule";
            string updatedName = testName + "2";

            using (HttpClient client = ApiHttpClient.Create()) {

                await EnsureEnabled(client);

                JObject webserverFeature = Utils.GetFeature(client, REWRITE_URL, "", null);
                Assert.NotNull(webserverFeature);

                JObject outboundRules = Utils.FollowLink(client, webserverFeature, "outbound_rules");
                string customTagsLink = Utils.GetLink(outboundRules, "rules");

                foreach (var p in client.Get(customTagsLink)["rules"].ToObject<IEnumerable<JObject>>()) {
                    if (p.Value<string>("name").Equals(testName, StringComparison.OrdinalIgnoreCase)
                            || p.Value<string>("name").Equals(updatedName, StringComparison.OrdinalIgnoreCase)) {
                        Assert.True(client.Delete(Utils.Self(p)));
                    }
                }

                JObject customTags = CreateCustomTags(client, webserverFeature, testName + "tags");
                JObject precondition = CreatePrecondition(client, webserverFeature, testName + "precondition");

                JObject outboundRule = JObject.FromObject(new {
                    name = testName,
                    match_type = "htmltags",
                    html_tags = new {
                        standard = new {
                            a = true,
                            area = true,
                            @base = true,
                            form = true,
                            frame = true,
                            head = true,
                            iframe = true,
                            img = true,
                            input = true,
                            link = true,
                            script = true
                        },
                        custom = customTags
                    },
                    pattern = "abc.aspx?a=b&c=d",
                    pattern_syntax = "regular_expression",
                    ignore_case = false,
                    negate = true,
                    stop_processing = false,
                    rewrite_value = "test rewrite value",
                    conditions = new object[] {
                            new {
                                input = "{URL}",
                                pattern = ".*",
                                negate = true,
                                ignore_case = false,
                                match_type = "pattern"
                            },
                        },
                    precondition = precondition,
                    outbound_rules = outboundRules
                });

                JObject result = client.Post(customTagsLink, outboundRule);

                Assert.NotNull(result);

                AssertOutboundRulesEqual(outboundRule, result);

                JObject updatedPrecondition = CreatePrecondition(client, webserverFeature, updatedName + "precondition");

                JObject updateOutboundRule = JObject.FromObject(new {
                    name = updatedName,
                    match_type = "servervariable",
                    server_variable = "abcdefg",
                    pattern = "abc.aspx",
                    pattern_syntax = "wildcard",
                    ignore_case = true,
                    negate = false,
                    stop_processing = true,
                    rewrite_value = "test rewrite update",
                    conditions = new object[] {
                            new {
                                input = "{CONTENT_TYPE}",
                                pattern = ".*",
                                negate = false,
                                ignore_case = true,
                                match_type = "pattern"
                            },
                        },
                    precondition = updatedPrecondition,
                    outbound_rules = outboundRules
                });

                result = client.Patch(Utils.Self(result), updateOutboundRule);

                Assert.NotNull(result);

                AssertOutboundRulesEqual(updateOutboundRule, result);

                Assert.True(client.Delete(Utils.Self(result)));
            }
        }

        [Fact]
        public async Task CreateAndUpdateProvider()
        {
            const string testProviderName = "TestProvider";
            string updatedTestProviderName = testProviderName + "2";

            using (HttpClient client = ApiHttpClient.Create()) {

                await EnsureEnabled(client);

                JObject webserverFeature = Utils.GetFeature(client, REWRITE_URL, "", null);
                Assert.NotNull(webserverFeature);

                JObject providers = Utils.FollowLink(client, webserverFeature, "providers");
                string providersLink = Utils.GetLink(providers, "entries");

                foreach (var m in client.Get(providersLink)["entries"].ToObject<IEnumerable<JObject>>()) {
                    if (m.Value<string>("name").Equals(testProviderName, StringComparison.OrdinalIgnoreCase)
                            || m.Value<string>("name").Equals(updatedTestProviderName, StringComparison.OrdinalIgnoreCase)) {
                        Assert.True(client.Delete(Utils.Self(m)));
                    }
                }

                JObject provider = JObject.FromObject(new {
                    name = testProviderName,
                    type = "testType",
                    settings = new object[] {
                            new {
                                key = "abc",
                                value = "def"
                            },
                            new {
                                key = "zyx",
                                value = "wvu"
                            }
                        },
                    providers = providers
                });

                JObject result = client.Post(providersLink, provider);

                Assert.NotNull(result);

                AssertProvidersEqual(provider, result);

                JObject updateProvider = JObject.FromObject(new {
                    name = updatedTestProviderName,
                    type = "testType22",
                    settings = new object[] {
                            new {
                                key = "igloo",
                                value = "anonymous"
                            },
                            new {
                                key = "tea",
                                value = "coffee"
                            },
                            new {
                                key = "record",
                                value = "paper"
                            }
                        },
                    providers = providers
                });

                result = client.Patch(Utils.Self(result), updateProvider);

                Assert.NotNull(result);

                AssertProvidersEqual(updateProvider, result);

                Assert.True(client.Delete(Utils.Self(result)));
            }
        }

        [Fact]
        public void Delegation()
        {
            string[] sections = {
                "allowed-server-variables",
                "global-rules",
                "inbound-rules",
                "outbound-rules",
                "providers",
                "rewrite-maps"
            };

            using (HttpClient client = ApiHttpClient.Create()) {

                EnsureEnabled(client).Wait();

                foreach (string section in sections) {

                    string url = REWRITE_URL + $"/{section}";

                    JObject feature = Utils.GetFeature(client, url, "", null);

                    string overrideMode = feature["metadata"].Value<string>("override_mode_effective") == "allow" ? "deny" : "allow";

                    object update = new {
                        metadata = new {
                            override_mode = overrideMode
                        }
                    };

                    JObject result = client.Patch(Utils.Self(feature), update);

                    Assert.True(result["metadata"].Value<string>("override_mode") == overrideMode);

                    update = new {
                        metadata = new {
                            override_mode = "inherit"
                        }
                    };

                    result = client.Patch(Utils.Self(result), update);

                    Assert.True(result["metadata"].Value<string>("override_mode") == "inherit");
                }
            }
        }



        private static void AssertInboundRulesEqual(JObject a, JObject b)
        {
            Assert.Equal(a.Value<string>("name"), b.Value<string>("name"));
            Assert.Equal(a.Value<string>("pattern"), b.Value<string>("pattern"));
            Assert.Equal(a.Value<string>("pattern_syntax"), b.Value<string>("pattern_syntax"));
            Assert.Equal(a.Value<bool>("ignore_case"), b.Value<bool>("ignore_case"));
            Assert.Equal(a.Value<bool>("negate"), b.Value<bool>("negate"));
            Assert.Equal(a.Value<bool>("stop_processing"), b.Value<bool>("stop_processing"));

            JObject action = a.Value<JObject>("action");
            JObject resultAction = b.Value<JObject>("action");

            Assert.Equal(action.Value<string>("type"), resultAction.Value<string>("type"));
            Assert.Equal(action.Value<bool>("append_query_string"), resultAction.Value<bool>("append_query_string"));
            Assert.Equal(action.Value<string>("url"), resultAction.Value<string>("url"));
            Assert.Equal(action.Value<string>("description"), resultAction.Value<string>("description"));
            Assert.Equal(action.Value<string>("reason"), resultAction.Value<string>("reason"));

            JObject[] aServerVariables = a["server_variables"].ToObject<JObject[]>();
            JObject[] bServerVariables = b["server_variables"].ToObject<JObject[]>();

            Assert.Equal(aServerVariables.Length, bServerVariables.Length);

            for (int i = 0; i < aServerVariables.Length; i++) {
                JObject aServerVariable = aServerVariables[i];
                JObject bServerVariable = bServerVariables[i];

                Assert.Equal(aServerVariable.Value<string>("name"), bServerVariable.Value<string>("name"));
                Assert.Equal(aServerVariable.Value<string>("value"), bServerVariable.Value<string>("value"));
                Assert.Equal(aServerVariable.Value<bool>("replace"), bServerVariable.Value<bool>("replace"));
            }

            JObject[] aConditions = a["conditions"].ToObject<JObject[]>();
            JObject[] bConditions = b["conditions"].ToObject<JObject[]>();

            Assert.Equal(aConditions.Length, bConditions.Length);

            for (int i = 0; i < aConditions.Length; i++) {
                JObject aCondition = aConditions[i];
                JObject bCondition = bConditions[i];

                Assert.Equal(aCondition.Value<string>("input"), bCondition.Value<string>("input"));
                Assert.Equal(aCondition.Value<string>("pattern"), bCondition.Value<string>("pattern"));
                Assert.Equal(aCondition.Value<bool>("negate"), bCondition.Value<bool>("negate"));
                Assert.Equal(aCondition.Value<bool>("ignore_case"), bCondition.Value<bool>("ignore_case"));
                Assert.Equal(aCondition.Value<string>("match_type"), bCondition.Value<string>("match_type"));
            }
        }

        private void AssertRewriteMapsEqual(JObject a, JObject b)
        {
            Assert.Equal(a.Value<string>("name"), b.Value<string>("name"));
            Assert.Equal(a.Value<string>("default_value"), b.Value<string>("default_value"));
            Assert.Equal(a.Value<bool>("ignore_case"), b.Value<bool>("ignore_case"));

            JObject[] aMaps = a["entries"].ToObject<JObject[]>();
            JObject[] bMaps = b["entries"].ToObject<JObject[]>();

            Assert.True(aMaps.Length == bMaps.Length);
            for (int i = 0; i < aMaps.Length; i++) {
                JObject m = aMaps[i];
                JObject rm = bMaps[i];

                Assert.Equal(m.Value<string>("key"), rm.Value<string>("key"));
                Assert.Equal(m.Value<string>("value"), rm.Value<string>("value"));
            }
        }

        private void AssertProvidersEqual(JObject a, JObject b)
        {
            Assert.Equal(a.Value<string>("name"), b.Value<string>("name"));
            Assert.Equal(a.Value<string>("type"), b.Value<string>("type"));

            JObject[] aSettings = a["settings"].ToObject<JObject[]>();
            JObject[] bSettings = b["settings"].ToObject<JObject[]>();

            Assert.True(aSettings.Length == bSettings.Length);
            for (int i = 0; i < aSettings.Length; i++) {
                JObject m = aSettings[i];
                JObject rm = bSettings[i];

                Assert.Equal(m.Value<string>("key"), rm.Value<string>("key"));
                Assert.Equal(m.Value<string>("value"), rm.Value<string>("value"));
            }
        }

        private void AssertPreconditionsEqual(JObject a, JObject b)
        {
            Assert.Equal(a.Value<string>("name"), b.Value<string>("name"));
            Assert.Equal(a.Value<string>("match"), b.Value<string>("match"));
            Assert.Equal(a.Value<string>("pattern_syntax"), b.Value<string>("pattern_syntax"));

            JObject[] aReqs = a["requirements"].ToObject<JObject[]>();
            JObject[] bReqs = b["requirements"].ToObject<JObject[]>();

            Assert.True(aReqs.Length == bReqs.Length);
            for (int i = 0; i < aReqs.Length; i++) {
                JObject aReq = aReqs[i];
                JObject bReq = bReqs[i];

                Assert.Equal(aReq.Value<string>("input"), bReq.Value<string>("input"));
                Assert.Equal(aReq.Value<string>("pattern"), bReq.Value<string>("pattern"));
                Assert.Equal(aReq.Value<bool>("negate"), bReq.Value<bool>("negate"));
                Assert.Equal(aReq.Value<bool>("ignore_case"), bReq.Value<bool>("ignore_case"));
            }
        }

        private void AssertCustomTagsEqual(JObject a, JObject b)
        {
            Assert.Equal(a.Value<string>("name"), b.Value<string>("name"));

            JObject[] aTags = a["tags"].ToObject<JObject[]>();
            JObject[] bTags = b["tags"].ToObject<JObject[]>();

            Assert.True(aTags.Length == bTags.Length);
            for (int i = 0; i < aTags.Length; i++) {
                JObject aTag = aTags[i];
                JObject bTag = bTags[i];

                Assert.Equal(aTag.Value<string>("name"), bTag.Value<string>("name"));
                Assert.Equal(aTag.Value<string>("attribute"), bTag.Value<string>("attribute"));
            }
        }

        private void AssertOutboundRulesEqual(JObject a, JObject b)
        {
            //
            // Root properties
            Assert.Equal(a.Value<string>("name"), b.Value<string>("name"));
            Assert.Equal(a.Value<string>("match_type"), b.Value<string>("match_type"));
            Assert.Equal(a.Value<string>("pattern"), b.Value<string>("pattern"));
            Assert.Equal(a.Value<string>("pattern_syntax"), b.Value<string>("pattern_syntax"));
            Assert.Equal(a.Value<bool>("ignore_case"), b.Value<bool>("ignore_case"));
            Assert.Equal(a.Value<bool>("negate"), b.Value<bool>("negate"));
            Assert.Equal(a.Value<bool>("stop_processing"), b.Value<bool>("stop_processing"));
            Assert.Equal(a.Value<string>("rewrite_value"), b.Value<string>("rewrite_value"));

            //
            // Conditions
            JObject[] aConditions = a["conditions"].ToObject<JObject[]>();
            JObject[] bConditions = b["conditions"].ToObject<JObject[]>();

            Assert.Equal(aConditions.Length, bConditions.Length);

            for (int i = 0; i < aConditions.Length; i++) {
                JObject aCondition = aConditions[i];
                JObject bCondition = bConditions[i];

                Assert.Equal(aCondition.Value<string>("input"), bCondition.Value<string>("input"));
                Assert.Equal(aCondition.Value<string>("pattern"), bCondition.Value<string>("pattern"));
                Assert.Equal(aCondition.Value<bool>("negate"), bCondition.Value<bool>("negate"));
                Assert.Equal(aCondition.Value<bool>("ignore_case"), bCondition.Value<bool>("ignore_case"));
                Assert.Equal(aCondition.Value<string>("match_type"), bCondition.Value<string>("match_type"));
            }

            //
            // Html Tags
            if (a["html_tags"] != null || b["html_tags"] != null) {
                JObject aStandard = a["html_tags"].Value<JObject>("standard");
                JObject bStandard = b["html_tags"].Value<JObject>("standard");

                Assert.Equal(aStandard.Value<bool>("a"), bStandard.Value<bool>("a"));
                Assert.Equal(aStandard.Value<bool>("area"), bStandard.Value<bool>("area"));
                Assert.Equal(aStandard.Value<bool>("base"), bStandard.Value<bool>("base"));
                Assert.Equal(aStandard.Value<bool>("form"), bStandard.Value<bool>("form"));
                Assert.Equal(aStandard.Value<bool>("frame"), bStandard.Value<bool>("frame"));
                Assert.Equal(aStandard.Value<bool>("head"), bStandard.Value<bool>("head"));
                Assert.Equal(aStandard.Value<bool>("iframe"), bStandard.Value<bool>("iframe"));
                Assert.Equal(aStandard.Value<bool>("img"), bStandard.Value<bool>("img"));
                Assert.Equal(aStandard.Value<bool>("input"), bStandard.Value<bool>("input"));
                Assert.Equal(aStandard.Value<bool>("link"), bStandard.Value<bool>("link"));
                Assert.Equal(aStandard.Value<bool>("script"), bStandard.Value<bool>("script"));

                if (a["html_tags"]["custom"] != null || b["html_tags"]["custom"] != null) {
                    Assert.Equal(a["html_tags"]["custom"].Value<string>("id"), b["html_tags"]["custom"].Value<string>("id"));
                }
            }

            //
            // Precondition
            if (a["precondition"] != null || b["precondition"] != null) {
                Assert.Equal(a["precondition"].Value<string>("id"), b["precondition"].Value<string>("id"));
            }
        }

        private JObject CreateCustomTags(HttpClient client, JObject feature, string name)
        {
            JObject outboundRules = Utils.FollowLink(client, feature, "outbound_rules");
            string customTagsLink = Utils.GetLink(outboundRules, "custom_tags");

            foreach (var p in client.Get(customTagsLink)["custom_tags"].ToObject<IEnumerable<JObject>>()) {
                if (p.Value<string>("name").Equals(name, StringComparison.OrdinalIgnoreCase)) {
                    Assert.True(client.Delete(Utils.Self(p)));
                }
            }

            JObject customTags = JObject.FromObject(new {
                name = name,
                tags = new object[] {
                            new {
                                name = "myHtmlTag",
                                attribute = "myHtmlAttribute"
                            },
                            new {
                                name = "anotherHtmlTag",
                                attribute = "anotherHtmlAttribute"
                            },
                            new {
                                name = $"{{{name}}}",
                                attribute = $"{{{name}}}"
                            }
                        },
                outbound_rules = outboundRules
            });

            return client.Post(customTagsLink, customTags);
        }

        private JObject CreatePrecondition(HttpClient client, JObject feature, string name)
        {
            JObject outboundRules = Utils.FollowLink(client, feature, "outbound_rules");
            string preConditionsLink = Utils.GetLink(outboundRules, "preconditions");

            foreach (var p in client.Get(preConditionsLink)["preconditions"].ToObject<IEnumerable<JObject>>()) {
                if (p.Value<string>("name").Equals(name, StringComparison.OrdinalIgnoreCase)) {
                    Assert.True(client.Delete(Utils.Self(p)));
                }
            }

            JObject precondition = JObject.FromObject(new {
                name = name,
                match = "all",
                pattern_syntax = "regular_expression",
                requirements = new object[] {
                            new {
                                input = "{RESPONSE_CONTENT_TYPE}",
                                pattern = "text/html",
                                negate = true,
                                ignore_case = true
                            },
                            new {
                                input = "{RESPONSE_CONTENT_LENGTH}",
                                pattern = "500",
                                negate = false,
                                ignore_case = false
                            },
                            new {
                                input = $"{{{name}}}",
                                pattern = $"{{{name}}}",
                                negate = false,
                                ignore_case = false
                            }
                        },
                outbound_rules = outboundRules
            });

            return client.Post(preConditionsLink, precondition);
        }

        private async Task EnsureEnabled(HttpClient client)
        {
            HttpResponseMessage response = await client.GetAsync(REWRITE_URL + "?scope=");

            if (Globals.Success(response)) {
                return;
            }

            Assert.NotNull(client.Post(REWRITE_URL, new { }));
        }

        private async Task<bool> AddAllowedServerVariable(string variable)
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                await EnsureEnabled(client);

                JObject webserverFeature = Utils.GetFeature(client, REWRITE_URL, "", null);

                JObject allowedServerVariablesResource = Utils.FollowLink(client, webserverFeature, "allowed_server_variables");

                var allowedVariables = allowedServerVariablesResource["entries"].ToObject<List<string>>();

                if (!allowedVariables.Any(v => v.Equals(variable, StringComparison.OrdinalIgnoreCase))) {
                    allowedVariables.Add(variable);
                    allowedServerVariablesResource["entries"] = JToken.FromObject(allowedVariables);
                    return client.Patch(Utils.Self(allowedServerVariablesResource), allowedServerVariablesResource) != null;
                }

                return true;
            }
        }
    }
}
