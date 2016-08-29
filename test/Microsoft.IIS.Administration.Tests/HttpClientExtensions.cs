// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Tests
{
    using System.Net.Http;
    using System.Text;

    public static class HttpClientExtensions
    {
        public static bool Get(this HttpClient client, string uri, out string result)
        {
            HttpResponseMessage responseMessage = client.GetAsync(uri).Result;
            result = responseMessage.Content.ReadAsStringAsync().Result;
            return Globals.Success(responseMessage);
        }

        public static bool Post(this HttpClient client, string uri, string body, out string result)
        {
            HttpContent content = new StringContent(body, Encoding.UTF8, "application/json");
            HttpResponseMessage response = client.PostAsync(uri, content).Result;

            result = response.Content.ReadAsStringAsync().Result;

            return Globals.Success(response);
        }

        public static bool Patch(this HttpClient client, string uri, string body, out string result)
        {
            HttpContent content = new StringContent(body, Encoding.UTF8, "application/json");
            HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod("PATCH"), uri) {
                Content = content
            };

            HttpResponseMessage response = client.SendAsync(requestMessage).Result;

            result = response.Content.ReadAsStringAsync().Result;

            return Globals.Success(response);
        }

        public static bool Delete(this HttpClient client, string uri)
        {
            HttpResponseMessage response = client.DeleteAsync(uri).Result;

            return Globals.Success(response);
        }
    }
}
