// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Microsoft.IIS.Administration.Core;
    using System;

    // Keep public for resolution of enums from 'dynamic' types in helper classes i.e. DynamicHelper
    public enum MatchType {
        Pattern = 0,
        IsFile = 1,
        IsDirectory = 2,
    }

    class MatchTypeHelper
    {
        public static string ToJsonModel(MatchType matchType)
        {
            switch (matchType) {
                case MatchType.Pattern:
                    return "pattern";
                case MatchType.IsFile:
                    return "is_file";
                case MatchType.IsDirectory:
                    return "is_directory";
                default:
                    throw new ArgumentException(nameof(matchType));
            }
        }

        public static MatchType FromJsonModel(string model)
        {
            switch (model.ToLowerInvariant()) {
                case "pattern":
                    return MatchType.Pattern;
                case "is_file":
                    return MatchType.IsFile;
                case "is_directory":
                    return MatchType.IsDirectory;
                default:
                    throw new ApiArgumentException("match_type");
            }
        }
    }
}

