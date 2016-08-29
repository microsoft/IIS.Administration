// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core.Http {
    using AspNetCore.Http;


    public static class HttpResponseExtensions {
        public static void SetItemsCount(this HttpResponse response, int count) {
            response.Headers[HeaderNames.Total_Count] = count.ToString();
        }
    }
}
