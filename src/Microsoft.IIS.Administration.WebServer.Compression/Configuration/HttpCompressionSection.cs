// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Compression
{
    using Microsoft.Web.Administration;

    public sealed class HttpCompressionSection : ConfigurationSection {

        private const string CacheControlHeaderAttribute = "cacheControlHeader";
        private const string DirectoryAttribute = "directory";
        private const string DoDiskSpaceLimitingAttribute = "doDiskSpaceLimiting";
        private const string MaxDiskSpaceUsageAttribute = "maxDiskSpaceUsage";
        private const string MinFileSizeForCompAttribute = "minFileSizeForComp";
        private const string NoCompressionForHttp10Attribute = "noCompressionForHttp10";
        private const string NoCompressionForProxiesAttribute = "noCompressionForProxies";
        private const string NoCompressionForRangeAttribute = "noCompressionForRange";
        private const string SendCacheHeadersAttribute = "sendCacheHeaders";

        public HttpCompressionSection() {
        }

        public string CacheControlHeader {
            get {
                return (string)base[CacheControlHeaderAttribute];
            }
            set {
                base[CacheControlHeaderAttribute] = value;
            }
        }

        public string Directory {
            get {
                return (string)base[DirectoryAttribute];
            }
            set {
                base[DirectoryAttribute] = value;
            }
        }

        public bool DoDiskSpaceLimiting {
            get {
                return (bool)base[DoDiskSpaceLimitingAttribute];
            }
            set {
                base[DoDiskSpaceLimitingAttribute] = value;
            }
        }

        public long MaxDiskSpaceUsage {
            get {
                return (long)base[MaxDiskSpaceUsageAttribute];
            }
            set {
                base[MaxDiskSpaceUsageAttribute] = value;
            }
        }

        public long MinFileSizeForComp {
            get {
                return (long)base[MinFileSizeForCompAttribute];
            }
            set {
                base[MinFileSizeForCompAttribute] = value;
            }
        }

        public bool NoCompressionForHttp10 {
            get {
                return (bool)base[NoCompressionForHttp10Attribute];
            }
            set {
                base[NoCompressionForHttp10Attribute] = value;
            }
        }

        public bool NoCompressionForProxies {
            get {
                return (bool)base[NoCompressionForProxiesAttribute];
            }
            set {
                base[NoCompressionForProxiesAttribute] = value;
            }
        }

        public bool NoCompressionForRange {
            get {
                return (bool)base[NoCompressionForRangeAttribute];
            }
            set {
                base[NoCompressionForRangeAttribute] = value;
            }
        }

        public bool SendCacheHeaders {
            get {
                return (bool)base[SendCacheHeadersAttribute];
            }
            set {
                base[SendCacheHeadersAttribute] = value;
            }
        }
    }
}
