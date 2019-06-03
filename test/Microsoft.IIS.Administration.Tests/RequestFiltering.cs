// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Tests
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Dynamic;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using Xunit;

    public class RequestFiltering
    {
        public static readonly string REQUEST_FILTERING_URL = $"{Configuration.Instance().TEST_SERVER_URL}/api/webserver/http-request-filtering";


        [Fact]
        public void ChangeAllProperties()
        {
            using(HttpClient client = ApiHttpClient.Create()) {

                JObject reqFilt = GetRequestFilteringFeature(client, null, null);
                JObject cachedFeature = new JObject(reqFilt);

                reqFilt["allow_unlisted_file_extensions"] = !reqFilt.Value<bool>("allow_unlisted_file_extensions");
                reqFilt["allow_unlisted_verbs"] = !reqFilt.Value<bool>("allow_unlisted_verbs");
                reqFilt["allow_high_bit_characters"] = !reqFilt.Value<bool>("allow_high_bit_characters");
                reqFilt["allow_double_escaping"] = !reqFilt.Value<bool>("allow_double_escaping");
                reqFilt["max_content_length"] = reqFilt.Value<long>("max_content_length") - 1;
                reqFilt["max_url_length"] = reqFilt.Value<long>("max_url_length") - 1;
                reqFilt["max_query_string_length"] = reqFilt.Value<long>("max_query_string_length") - 1;

                JObject verb = JObject.FromObject(new {
                    allowed = false,
                    name = "test_verb"
                });

                reqFilt.Value<JArray>("verbs").Add(verb);

                string result;
                string body = JsonConvert.SerializeObject(reqFilt);

                Assert.True(client.Patch(Utils.Self(reqFilt), body, out result));

                JObject newReqFilt = JsonConvert.DeserializeObject<JObject>(result);

                Assert.True(Utils.JEquals<bool>(reqFilt, newReqFilt, "allow_unlisted_file_extensions"));
                Assert.True(Utils.JEquals<bool>(reqFilt, newReqFilt, "allow_unlisted_verbs"));
                Assert.True(Utils.JEquals<bool>(reqFilt, newReqFilt, "allow_high_bit_characters"));
                Assert.True(Utils.JEquals<bool>(reqFilt, newReqFilt, "allow_double_escaping"));
                Assert.True(Utils.JEquals<long>(reqFilt, newReqFilt, "max_content_length"));
                Assert.True(Utils.JEquals<long>(reqFilt, newReqFilt, "max_url_length"));
                Assert.True(Utils.JEquals<long>(reqFilt, newReqFilt, "max_query_string_length"));

                var verbs = newReqFilt.Value<JArray>("verbs");

                JObject targetVerb = null;
                foreach(var v in verbs) {
                    if(v.Value<string>("name").Equals(verb.Value<string>("name"))) {
                        targetVerb = (JObject)v;
                    }
                }

                Assert.NotNull(targetVerb);

                // Create json payload of original feature state
                body = JsonConvert.SerializeObject(cachedFeature);

                // Patch request filtering to original state
                Assert.True(client.Patch(Utils.Self(newReqFilt), body, out result));
            }
        }

        [Fact]
        public void CreateCheckConflictRemoveFileNameExtension()
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                dynamic fileExtension = new ExpandoObject();
                fileExtension.extension = "test_ext";
                fileExtension.allowed = false;


                CreateCheckConflictRemove(client, "file_extensions", fileExtension);
            }
        }

        [Fact]
        public void CreatePatchRemoveFileNameExtension()
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                dynamic fileExtension = new ExpandoObject();
                fileExtension.extension = "test_ext";
                fileExtension.allowed = false;

                JObject ext = Create(client, "file_extensions", fileExtension);

                string result;
                ext["extension"] = "test_ext-new";

                Assert.True(client.Patch(Utils.Self(ext), JsonConvert.SerializeObject(ext), out result));

                JObject newExt = JsonConvert.DeserializeObject<JObject>(result);

                Assert.True(Utils.JEquals<string>(ext, newExt, "extension"));

                Assert.True(client.Delete(Utils.Self(newExt)));
            }
        }

        [Fact]
        public void CreateCheckConflictRemoveHeaderLimit()
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                dynamic headerLimit = new ExpandoObject();
                headerLimit.header = "test_header";
                headerLimit.size_limit = 64;

                CreateCheckConflictRemove(client, "header_limits", headerLimit);
            }
        }

        [Fact]
        public void CreatePatchRemoveHeaderLimit()
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                dynamic headerLimit = new ExpandoObject();
                headerLimit.header = "test_header";
                headerLimit.size_limit = 64;

                JObject hl = Create(client, "header_limits", headerLimit);

                string result;
                hl["header"] = "test_header-new";

                Assert.True(client.Patch(Utils.Self(hl), JsonConvert.SerializeObject(hl), out result));

                JObject newHl = JsonConvert.DeserializeObject<JObject>(result);

                Assert.True(Utils.JEquals<string>(hl, newHl, "header"));

                Assert.True(client.Delete(Utils.Self(newHl)));
            }
        }

        [Fact]
        public void CreateRemoveHiddenSegment()
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                dynamic hiddenSegment = new ExpandoObject();
                hiddenSegment.segment = "test_h_segment";

                CreateCheckConflictRemove(client, "hidden_segments", hiddenSegment);
            }
        }

        [Fact]
        public void CreateCheckConflictRemoveQueryString()
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                dynamic queryString = new ExpandoObject();
                queryString.query_string = "test_q_string";
                queryString.allow = false;

                CreateCheckConflictRemove(client, "query_strings", queryString);
            }
        }

        [Fact]
        public void CreatePatchRemoveQueryString()
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                dynamic queryString = new ExpandoObject();
                queryString.query_string = "test_q_string";
                queryString.allow = false;

                JObject qs = Create(client, "query_strings", queryString);

                string result;
                qs["query_string"] = "test_q_string-new";

                Assert.True(client.Patch(Utils.Self(qs), JsonConvert.SerializeObject(qs), out result));

                JObject newQs = JsonConvert.DeserializeObject<JObject>(result);

                Assert.True(Utils.JEquals<string>(qs, newQs, "query_string"));

                Assert.True(client.Delete(Utils.Self(newQs)));
            }
        }

        [Fact]
        public void CreateCheckConflictRemoveRule()
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                dynamic rule = new ExpandoObject();
                rule.name = "test_rule";

                CreateCheckConflictRemove(client, "rules", rule);

                rule = new ExpandoObject();
                rule.name = "test_rule";
                rule.scan_url = true;
                rule.scan_query_string = true;
                rule.headers = new string[] { "h1","h2" };
                rule.applies_to = new string[] { ".e1", ".e2" };
                rule.deny_strings = new string[] { "rand", "str" };

                CreateCheckConflictRemove(client, "rules", rule);
            }
        }

        [Fact]
        public void CreatePatchRemoveRule()
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                dynamic rule = new ExpandoObject();
                rule.name = "test_rule";
                rule.scan_url = true;
                rule.scan_query_string = true;
                rule.headers = new string[] { "h1", "h2" };
                rule.applies_to = new string[] { ".e1", ".e2" };
                rule.deny_strings = new string[] { "rand", "str" };

                JObject r = Create(client, "rules", rule);

                string result;
                r["name"] = "test_rule-new";

                Assert.True(client.Patch(Utils.Self(r), JsonConvert.SerializeObject(r), out result));

                JObject newRule = JsonConvert.DeserializeObject<JObject>(result);

                Assert.True(Utils.JEquals<string>(r, newRule, "name"));

                Assert.True(client.Delete(Utils.Self(newRule)));
            }
        }

        [Fact]
        public void CreateCheckConflictRemoveUrl()
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                dynamic url = new ExpandoObject();
                url.url = "test_url";
                url.allow = false;

                CreateCheckConflictRemove(client, "urls", url);
            }
        }

        [Fact]
        public void CreatePatchRemoveUrl()
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                dynamic url = new ExpandoObject();
                url.url = "test_url";
                url.allow = false;

                JObject u = Create(client, "urls", url);

                string result;
                u["url"] = "test_url-new";

                Assert.True(client.Patch(Utils.Self(u), JsonConvert.SerializeObject(u), out result));

                JObject newUrl = JsonConvert.DeserializeObject<JObject>(result);

                Assert.True(Utils.JEquals<string>(u, newUrl, "url"));

                Assert.True(client.Delete(Utils.Self(newUrl)));
            }
        }

        private void CreateCheckConflictRemove(HttpClient client, string linkName, ExpandoObject obj, JObject requestFilteringFeature = null)
        {
            if(requestFilteringFeature == null) {
                requestFilteringFeature = GetRequestFilteringFeature(client, null, null);
            }

            string link = Utils.GetLink(requestFilteringFeature, linkName);

            dynamic dynamicObj = obj;
            dynamicObj.request_filtering = requestFilteringFeature;

            string result;
            Assert.True(client.Post(link, JsonConvert.SerializeObject(obj), out result));

            JObject newObject = JsonConvert.DeserializeObject<JObject>(result);
            
            // Try to post same object again, ensuring we are returning conflict status code
            HttpContent content = new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json");
            HttpResponseMessage response = client.PostAsync(link, content).Result;

            Assert.True(response.StatusCode == HttpStatusCode.Conflict);

            Assert.True(client.Delete(Utils.Self(newObject)));
            Assert.False(client.Get(Utils.Self(newObject), out result));
        }

        private JObject Create(HttpClient client, string linkName, ExpandoObject obj, JObject requestFilteringFeature = null)
        {
            if (requestFilteringFeature == null) {
                requestFilteringFeature = GetRequestFilteringFeature(client, null, null);
            }

            string link = Utils.GetLink(requestFilteringFeature, linkName);

            dynamic dynamicObj = obj;
            dynamicObj.request_filtering = requestFilteringFeature;

            string result;
            Assert.True(client.Post(link, JsonConvert.SerializeObject(obj), out result));

            return JsonConvert.DeserializeObject<JObject>(result);
        }


        public static JObject GetRequestFilteringFeature(HttpClient client, string siteName, string path)
        {
            if (path != null) {
                if (!path.StartsWith("/")) {
                    throw new ArgumentException("path");
                }
            }

            string content;
            if (!client.Get(REQUEST_FILTERING_URL + "?scope=" + siteName + path, out content)) {
                return null;
            }

            return Utils.ToJ(content);
        }
    }
}
