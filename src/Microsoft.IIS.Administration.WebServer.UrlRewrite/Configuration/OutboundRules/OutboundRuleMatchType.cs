// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Microsoft.IIS.Administration.Core;
    using System;

    // Keep public for resolution of enums from 'dynamic' types in helper classes i.e. DynamicHelper
    public enum OutboundRuleMatchType {
        ServerVariable,
        Tags
    }

    static class OutboundMatchTypeHelper
    {
        public static string ToJsonModel(OutboundRuleMatchType matchType)
        {
            switch (matchType) {
                case OutboundRuleMatchType.ServerVariable:
                    return "server_variable";
                case OutboundRuleMatchType.Tags:
                    return "tags";
                default:
                    throw new ArgumentException(nameof(matchType));
            }
        }

        public static OutboundRuleMatchType FromJsonModel(string model)
        {
            switch (model.ToLowerInvariant()) {
                case "server_variable":
                    return OutboundRuleMatchType.ServerVariable;
                case "tags":
                    return OutboundRuleMatchType.Tags;
                default:
                    throw new ApiArgumentException("match_type");
            }
        }
    }
}

