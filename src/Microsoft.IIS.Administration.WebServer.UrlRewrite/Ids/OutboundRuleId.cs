// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using System;

    class OutboundRuleId
    {
        private const string PURPOSE = "WebServer.UrlRewrite.OutboundRule";
        private const char DELIMITER = '\n';

        private const uint PATH_INDEX = 0;
        private const uint SITE_ID_INDEX = 1;
        private const uint NAME_INDEX = 2;

        public string Name { get; private set; }
        public string Path { get; private set; }
        public long? SiteId { get; private set; }
        public string Uuid { get; private set; }

        public OutboundRuleId(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) {
                throw new ArgumentNullException("uuid");
            }

            string[] info = Core.Utils.Uuid.Decode(uuid, PURPOSE).Split(DELIMITER);

            string path = info[PATH_INDEX];
            string siteId = info[SITE_ID_INDEX];
            string name = info[NAME_INDEX];

            if (!string.IsNullOrEmpty(path)) {

                if (string.IsNullOrEmpty(siteId)) {
                    throw new ArgumentNullException("siteId");
                }

                this.Path = path;
                this.SiteId = long.Parse(siteId);
            }
            else if (!string.IsNullOrEmpty(siteId)) {
                this.SiteId = long.Parse(siteId);
            }

            this.Name = name;
            this.Uuid = uuid;
        }

        public OutboundRuleId(long? siteId, string path, string name)
        {
            if (!string.IsNullOrEmpty(path) && siteId == null) {
                throw new ArgumentNullException("siteId");
            }

            this.Path = path;
            this.SiteId = siteId;
            this.Name = name;

            string encodableSiteId = this.SiteId == null ? "" : this.SiteId.Value.ToString();

            this.Uuid = Core.Utils.Uuid.Encode($"{this.Path}{DELIMITER}{encodableSiteId}{DELIMITER}{this.Name}", PURPOSE);
        }
    }
}
