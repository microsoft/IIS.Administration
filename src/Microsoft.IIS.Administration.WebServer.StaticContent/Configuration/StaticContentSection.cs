// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.StaticContent
{
    using Microsoft.Web.Administration;
    using System;
    using System.Globalization;
    using static MimeTypesGlobals;

    public sealed class StaticContentSection : ConfigurationSection {

        private MimeMapCollection _collection;
        private HttpClientCacheElement _clientCache;
        private const string ClientCacheElement = "clientCache";
        private const string DefaultDocFooterAttribute = "defaultDocFooter";
        private const string IsDocFooterFileNameAttribute = "isDocFooterFileName";
        private const string EnableDocFooterAttribute = "enableDocFooter";

        public StaticContentSection() {
        }

        public HttpClientCacheElement ClientCache {
            get {
                if (_clientCache == null) {
                    _clientCache = (HttpClientCacheElement)GetChildElement(ClientCacheElement, typeof(HttpClientCacheElement));
                }

                return _clientCache;
            }
        }

        public MimeMapCollection MimeMaps {
            get {
                if (_collection == null) {
                    _collection = (MimeMapCollection)GetCollection(typeof(MimeMapCollection));
                }

                return _collection;
            }
        }

        public string DefaultDocFooter
        {
            get
            {
                return (string)base[DefaultDocFooterAttribute];
            }
            set
            {
                base[DefaultDocFooterAttribute] = value;
            }
        }

        public bool IsDocFooterFileName
        {
            get
            {
                return (bool)base[IsDocFooterFileNameAttribute];
            }
            set
            {
                base[IsDocFooterFileNameAttribute] = value;
            }
        }

        public bool EnableDocFooter
        {
            get
            {
                return (bool)base[EnableDocFooterAttribute];
            }
            set
            {
                base[EnableDocFooterAttribute] = value;
            }
        }


        public sealed class HttpClientCacheElement : ConfigurationElement {

            private const string CacheControlModeAttribute = "cacheControlMode";
            private const string HttpExpiresAttribute = "httpExpires";
            private const string CacheControlMaxAgeAttribute = "cacheControlMaxAge";
            private const string CacheControlCustomAttribute = "cacheControlCustom";
            private const string SetETagAttribute = "setEtag";

            public TimeSpan CacheControlMaxAge {
                get {
                    return (TimeSpan)base[CacheControlMaxAgeAttribute];
                }
                set {
                    base[CacheControlMaxAgeAttribute] = value;
                }
            }

            public HttpCacheControlMode CacheControlMode {
                get {
                    return (HttpCacheControlMode)base[CacheControlModeAttribute];
                }
                set {
                    base[CacheControlModeAttribute] = (int)value;
                }
            }

            public DateTime HttpExpires {
                get {
                    string value = (string)base[HttpExpiresAttribute];

                    if (String.IsNullOrEmpty(value)) {
                        return DateTime.Today.Add(TimeSpan.FromDays(3));
                    }

                    return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                }
                set {
                    base[HttpExpiresAttribute] = value.ToString("r", CultureInfo.InvariantCulture);
                }
            }

            public string CacheControlCustom
            {
                get
                {
                    return (string)base[CacheControlCustomAttribute];
                }
                set
                {
                    base[CacheControlCustomAttribute] = value;
                }
            }

            public bool SetETag
            {
                get
                {
                    return (bool)base[SetETagAttribute];
                }
                set
                {
                    base[SetETagAttribute] = value;
                }
            }
        }
    }
}
