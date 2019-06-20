
using System;
using System.Net.Http;
using Xunit;

namespace Microsoft.IIS.Administration.Tests.Asserts {
    public static class HttpAssertions
    {
        public static Action<HttpResponseMessage> Success
            = (HttpResponseMessage msg) => {
                Assert.True((int)msg.StatusCode >= 200, $"{msg.ToString()}");
                Assert.True((int)msg.StatusCode < 300, $"{msg.ToString()}");
            };
    }
}