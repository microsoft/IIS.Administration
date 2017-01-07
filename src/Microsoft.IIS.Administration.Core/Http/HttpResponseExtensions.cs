// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core.Http {
    using AspNetCore.Http;
    using System.Text.Encodings.Web;

    public static class HttpResponseExtensions {
        public static void SetItemsCount(this HttpResponse response, int count) {
            response.Headers[HeaderNames.Total_Count] = count.ToString();
        }

        public static void SetContentDisposition(this IHeaderDictionary headers, bool inline, string fileName) {
            const string i = "inline", a = "attachment";
            headers[HeaderNames.ContentDisposition] = $"{(inline ? i : a)};filename={UrlEncoder.Default.Encode(fileName)}";
        }

        public static void SetContentRange(this IHeaderDictionary headers, long start, long finish, long length)
        {
            headers[HeaderNames.ContentRange] = $"{start}-{finish}/{length}";
        }
    }
}
