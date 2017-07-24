// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Microsoft.IIS.Administration.Core;
    using System;

    // Keep public for resolution of enums from 'dynamic' types in helper classes i.e. DynamicHelper
    public enum LogicalGrouping {
        MatchAll = 0,
        MatchAny = 1,
    }

    static class LogicalGroupingHelper
    {
        public static string ToJsonModel(LogicalGrouping logicalGrouping)
        {
            switch (logicalGrouping) {
                case LogicalGrouping.MatchAll:
                    return "match_all";
                case LogicalGrouping.MatchAny:
                    return "match_any";
                default:
                    throw new ArgumentException(nameof(logicalGrouping));
            }
        }

        public static LogicalGrouping FromJsonModel(string model)
        {
            switch (model.ToLowerInvariant()) {
                case "match_all":
                    return LogicalGrouping.MatchAll;
                case "match_any":
                    return LogicalGrouping.MatchAny;
                default:
                    throw new ApiArgumentException("condition_match_constraints");
            }
        }
    }
}

