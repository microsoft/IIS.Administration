// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRequestTracing
{
    using System;

    public class RuleId
    {
        private const string PURPOSE = "WebServer.HttpRequestTracing.Rule";
        private const char DELIMITER = '\n';

        private const uint URL_INDEX = 0;
        private const uint CONFIG_PATH_INDEX = 1;
        private const uint SITE_ID_INDEX = 2;

        public string Path { get; private set; }
        public string AppPath { get; private set; }
        public long? SiteId { get; private set; }
        public string Uuid { get; private set; }

        public RuleId(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) {
                throw new ArgumentNullException("uuid");
            }

            var info = Core.Utils.Uuid.Decode(uuid, PURPOSE).Split(DELIMITER);

            string url = info[URL_INDEX];
            string configPath = info[CONFIG_PATH_INDEX];
            string siteId = info[SITE_ID_INDEX];

            if (!string.IsNullOrEmpty(configPath)) {

                if (string.IsNullOrEmpty(siteId)) {
                    throw new ArgumentNullException("siteId");
                }

                this.AppPath = configPath;
                this.SiteId = long.Parse(siteId);
            }

            else if (!string.IsNullOrEmpty(siteId)) { 

                this.SiteId = long.Parse(siteId);
            }

            this.Path = url;
            this.Uuid = uuid;
        }

        public RuleId(long? siteId, string configPath, string path)
        {

            if (string.IsNullOrEmpty(path)) {
                throw new ArgumentNullException("url");
            }

            if (!string.IsNullOrEmpty(configPath) && siteId == null) {
                throw new ArgumentNullException("siteId");
            }

            this.Path = path;
            this.AppPath = configPath;
            this.SiteId = siteId;

            this.Uuid = Core.Utils.Uuid.Encode(this.Path + DELIMITER + this.AppPath + DELIMITER + (this.SiteId == null ? "" : this.SiteId.Value.ToString()), PURPOSE);
        }
    }
}
