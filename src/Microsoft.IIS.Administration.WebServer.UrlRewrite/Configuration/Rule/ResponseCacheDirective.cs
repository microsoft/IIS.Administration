// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Microsoft.IIS.Administration.Core;
    using System;

    // Keep public for resolution of enums from 'dynamic' types in helper classes i.e. DynamicHelper
    public enum ResponseCacheDirective
    {
        Auto = 0,
        Always = 1,
        Never = 2,
        NotIfRuleMatched = 3
    }

    static class ResponseCacheDirectiveHelper
    {
        public static string ToJsonModel(ResponseCacheDirective responseCacheDirective)
        {
            switch (responseCacheDirective) {
                case ResponseCacheDirective.Auto:
                    return "auto";
                case ResponseCacheDirective.Always:
                    return "always";
                case ResponseCacheDirective.Never:
                    return "never";
                case ResponseCacheDirective.NotIfRuleMatched:
                    return "not_if_rule_matched";
                default:
                    throw new ArgumentException(nameof(responseCacheDirective));
            }
        }

        public static ResponseCacheDirective FromJsonModel(string model)
        {
            switch (model.ToLowerInvariant()) {
                case "auto":
                    return ResponseCacheDirective.Auto;
                case "always":
                    return ResponseCacheDirective.Always;
                case "never":
                    return ResponseCacheDirective.Never;
                case "not_if_rule_matched":
                    return ResponseCacheDirective.NotIfRuleMatched;
                default:
                    throw new ApiArgumentException("response_cache_directive");
            }
        }
    }
}

