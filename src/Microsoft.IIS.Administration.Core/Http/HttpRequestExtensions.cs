// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core.Http
{
    using AspNetCore.Http;
    using System;
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

        public static bool TryGetRange(this IHeaderDictionary requestHeaders, out long start, out long finish, long maxLength, string units = "bytes")
        {
            //
            // Validate
            // Range: {units}={start}-{finish}

            start = finish = -1;
            string sstart = null, sfinish = null, prefix = $"{units}=";
            string range = requestHeaders[HeaderNames.Range].ToString().Trim(' ');

            if (range.IndexOf(prefix, StringComparison.OrdinalIgnoreCase) == 0) {
                range = range.Remove(0, prefix.Length);
                var parts = range.Split('-');
                if (parts.Length == 2) {
                    sstart = parts[0];
                    sfinish = parts[1];
                }
            }

            if (!long.TryParse(sstart, out start) ||
                    !long.TryParse(sfinish, out finish) ||
                    start < 0 ||
                    finish >= maxLength ||
                    start > finish) {
                return false;
            }

            return true;
        }
    }
}
