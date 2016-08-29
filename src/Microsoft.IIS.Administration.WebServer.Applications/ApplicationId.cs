// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Applications
{
    using System;

    public sealed class ApplicationId
    {
        private const string PURPOSE = "WebServer.Application";
        private const char DELIMITER = '\n';
        private const uint SITE_ID_NUM_INDEX = 0;
        private const uint APP_NAME_INDEX = 1;

        public long SiteId { get; private set; }
        public string Path { get; private set; }
        public string Uuid { get; private set; }


        public ApplicationId(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) {
                throw new ArgumentNullException("uuid");
            }

            var info = Core.Utils.Uuid.Decode(uuid, PURPOSE).Split(DELIMITER);

            this.SiteId = long.Parse(info[SITE_ID_NUM_INDEX]);
            this.Path = info[APP_NAME_INDEX];
            this.Uuid = uuid;
        }

        public ApplicationId(string name, long siteId)
        {
            if (string.IsNullOrEmpty(name)) {
                throw new ArgumentNullException("name");
            }

            this.SiteId = siteId;
            this.Path = name;
            this.Uuid = Core.Utils.Uuid.Encode($"{SiteId}{DELIMITER}{Path}", PURPOSE);
        }
    }
}
