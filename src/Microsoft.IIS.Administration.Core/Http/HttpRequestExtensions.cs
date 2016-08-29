// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core.Http
{
    using AspNetCore.Http;
    using Utils;

    public static class HttpRequestxtensions
    {
        public static Fields GetFields(this HttpRequest request) {
            string fieldsQuery = request.Query["fields"];
            return new Fields(fieldsQuery != null ? fieldsQuery.Split(',') : null);
        }

        public static Filter GetFilter(this HttpRequest request)
        {
            return new Filter(request.Query);
        }
    }
}
