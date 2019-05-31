// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Tests
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.Net.Http;
    using System.Text;
    using Xunit;
    using Xunit.Abstractions;

    public class Transactions
    {
        private const string TRANSACTION_SITE_NAME = "trans_site";
        public const string TRANSACTION_HEADER = "Transaction-Id";
        public static readonly string TRANSACTIONS_URL = $"{Configuration.Instance().TEST_SERVER_URL}/api/webserver/transactions";

        private ITestOutputHelper _output;

        public Transactions(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void UseTransactionManipulateSite()
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                // Ensure a site with the name of the transaction test site does not exist.
                Sites.EnsureNoSite(client, TRANSACTION_SITE_NAME);

                // Create the site we will be manipulating to test transactions
                JObject site = Sites.CreateSite(_output, client, TRANSACTION_SITE_NAME, 50000, Sites.TEST_SITE_PATH);
                Assert.NotNull(site);

                // Cache the value of the property we will be manipulating through a transaciton
                bool cachedAutoStart = site.Value<bool>("server_auto_start");

                // Create a transaction
                string res = null;
                Assert.True(client.Post(TRANSACTIONS_URL, "{}", out res));

                JObject transaction = JsonConvert.DeserializeObject<JObject>(res);

                // Create a request to manipulate the test site
                HttpRequestMessage req = new HttpRequestMessage(new HttpMethod("PATCH"), Utils.Self(site));

                // Add the transaction header to specify that we want to utilize the transaction in our patch request
                req.Headers.Add(TRANSACTION_HEADER, transaction.Value<string>("id"));
                site["server_auto_start"] = !site.Value<bool>("server_auto_start");
                req.Content = new StringContent(JsonConvert.SerializeObject(site), Encoding.UTF8, "application/json");

                // Patch the test site using a transaction
                var response = client.SendAsync(req).Result;

                Assert.True(Globals.Success(response));

                site = JsonConvert.DeserializeObject<JObject>(response.Content.ReadAsStringAsync().Result);

                // Check the value of the server auto start property of the test site after manipulating it through transaction
                bool transactionAutoStart = site.Value<bool>("server_auto_start");

                // Value should be different than the original value that we cached
                Assert.True(transactionAutoStart != cachedAutoStart);

                // Get the site without specifying the transaction, which means it should look the same as the original.
                Assert.True(client.Get(Utils.Self(site), out res));
                site = JsonConvert.DeserializeObject<JObject>(res);

                bool nonTransactionAutoStart = site.Value<bool>("server_auto_start");

                // Value should be the same as original value that we cached
                Assert.True(nonTransactionAutoStart == cachedAutoStart);

                // Create a request to commit the transaction
                req = new HttpRequestMessage(new HttpMethod("PATCH"), Utils.Self(transaction));

                // Specify the current transaction in the headers
                req.Headers.Add(TRANSACTION_HEADER, transaction.Value<string>("id"));
                req.Content = new StringContent(JsonConvert.SerializeObject(new { state = "committed"}), Encoding.UTF8, "application/json");

                // Patch the transaction to commit it
                response = client.SendAsync(req).Result;

                Assert.True(Globals.Success(response));

                // Get the transactions for the webserver
                Assert.True(client.Get(TRANSACTIONS_URL, out res));

                JArray transactions = JsonConvert.DeserializeObject<JObject>(res).Value<JArray>("transactions");

                // There should be no transactions after we commit ours
                Assert.True(transactions.Count == 0);

                // Get the site after committing the transaction so the server auto start should retain the manipulated value
                Assert.True(client.Get(Utils.Self(site), out res));
                site = JsonConvert.DeserializeObject<JObject>(res);

                bool commitedAutoStart = site.Value<bool>("server_auto_start");

                // Value should be different than the original value that we cached
                Assert.True(commitedAutoStart != cachedAutoStart);

                // Remove the test site we created
                Sites.EnsureNoSite(client, TRANSACTION_SITE_NAME);
            }
        }

        [Fact]
        public void EnsureTransactionBlocks()
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                // Ensure a site with the name of the transaction test site does not exist.
                Sites.EnsureNoSite(client, TRANSACTION_SITE_NAME);

                // Create the site we will be manipulating to test transactions
                JObject site = Sites.CreateSite(_output, client, TRANSACTION_SITE_NAME, 50000, Sites.TEST_SITE_PATH);
                Assert.NotNull(site);
                
                // Create a transaction
                string res = null;
                Assert.True(client.Post(TRANSACTIONS_URL, "{}", out res));

                JObject transaction = JsonConvert.DeserializeObject<JObject>(res);

                // Try to delete the site without specifying transaction
                var response = client.DeleteAsync(Utils.Self(site)).Result;

                // Not specifying the transaction should prevent us from deleting the site
                Assert.True(!Globals.Success(response));

                // Create a request to abort the transaction
                var req = new HttpRequestMessage(new HttpMethod("PATCH"), Utils.Self(transaction));

                // Specify the current transaction in the headers
                req.Headers.Add(TRANSACTION_HEADER, transaction.Value<string>("id"));
                req.Content = new StringContent(JsonConvert.SerializeObject(new { state = "aborted" }), Encoding.UTF8, "application/json");

                // Patch the transaction to abort it
                response = client.SendAsync(req).Result;

                Assert.True(Globals.Success(response));

                // Remove the test site
                Sites.EnsureNoSite(client, TRANSACTION_SITE_NAME);
            }
        }
    }
}
