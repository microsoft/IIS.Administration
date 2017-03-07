// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRequestTracing
{
    using System;

    public class TraceId
    {
        private const string PURPOSE = "WebServer.HttpRequestTracing.Trace";
        private const char DELIMITER = '\n';
        
        private const uint SITE_ID_INDEX = 0;
        private const uint NAME_INDEX = 1;

        public long SiteId { get; private set; }
        public string Name { get; private set; }
        public string Uuid { get; private set; }

        public TraceId(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) {
                throw new ArgumentNullException("uuid");
            }

            var info = Core.Utils.Uuid.Decode(uuid, PURPOSE).Split(DELIMITER);

            this.Name = info[NAME_INDEX];
            this.SiteId = long.Parse(info[SITE_ID_INDEX]);
            this.Uuid = uuid;
        }

        public TraceId(long siteId, string name)
        {
            this.SiteId = siteId;
            this.Name = name;
            this.Uuid = Core.Utils.Uuid.Encode(this.SiteId.ToString() + DELIMITER + this.Name, PURPOSE);
        }
    }
}
