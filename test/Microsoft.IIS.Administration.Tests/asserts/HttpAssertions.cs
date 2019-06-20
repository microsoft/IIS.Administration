// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using Xunit;

namespace Microsoft.IIS.Administration.Tests.Asserts {
    public static class HttpAssertions
    {
        public static Action<HttpResponseMessage> Success
            = (HttpResponseMessage msg) => {
                Assert.True((int)msg.StatusCode >= 200, $"{Format(msg)}");
                Assert.True((int)msg.StatusCode < 300, $"{Format(msg)}");
            };

        private static string Format(HttpResponseMessage msg)
        {
            try
            {
                string content = msg.Content.ReadAsStringAsync().Result;
                return msg.ToString() + "\nContent:" + content;
            } catch (Exception e)
            {
                return msg.ToString() + "\nError parsing content:" + e.ToString();
            }
        }
    }
}
