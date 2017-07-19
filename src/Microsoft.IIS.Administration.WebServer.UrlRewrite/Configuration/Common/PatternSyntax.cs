// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Microsoft.IIS.Administration.Core;
    using System;

    // Keep public for resolution of enums from 'dynamic' types in helper classes i.e. DynamicHelper
    public enum PatternSyntax
    {
        ECMAScript = 0,
        Wildcard = 1,
        ExactMatch = 2,
    }

    static class PatternSyntaxHelper
    {
        public static string ToJsonModel(PatternSyntax patternSyntax)
        {
             switch (patternSyntax) {
                case PatternSyntax.ECMAScript:
                    return "regular_expression";
                case PatternSyntax.Wildcard:
                    return "wildcard";
                case PatternSyntax.ExactMatch:
                    return "exact_match";
                default:
                    throw new ArgumentException(nameof(patternSyntax));
            }
        }

        public static PatternSyntax FromJsonModel(string model)
        {
            switch (model.ToLowerInvariant()) {
                case "regular_expression":
                    return PatternSyntax.ECMAScript;
                case "wildcard":
                    return PatternSyntax.Wildcard;
                case "exact_match":
                    return PatternSyntax.ExactMatch;
                default:
                    throw new ApiArgumentException("pattern_syntax");
            }
        }
    }
}

