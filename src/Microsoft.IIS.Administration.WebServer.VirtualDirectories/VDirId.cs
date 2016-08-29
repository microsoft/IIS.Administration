// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.VirtualDirectories
{
    using System;

    public class VDirId
    {
        private const string PURPOSE = "WebServer.VirtualDirectories";
        private const char DELIMITER = '\n';
        private const uint PATH_INDEX = 0;
        private const uint APP_PATH_INDEX = 1;
        private const uint SITE_ID_NUM_INDEX = 2;

        public string Path { get; private set; }
        public string AppPath { get; private set; }
        public long SiteId { get; private set; }
        public string Uuid { get; private set; }

        public VDirId(string uuid)
        {
            if(String.IsNullOrEmpty(uuid)) {
                throw new ArgumentNullException("uuid");
            }
            string[] info = Core.Utils.Uuid.Decode(uuid, PURPOSE).Split(DELIMITER);

            string path = info[PATH_INDEX];
            string appPath = info[APP_PATH_INDEX];
            string siteId = info[SITE_ID_NUM_INDEX];

            this.SiteId = long.Parse(info[SITE_ID_NUM_INDEX]);
            this.AppPath = info[APP_PATH_INDEX];
            this.Path = info[PATH_INDEX];
            this.Uuid = uuid;
        }

        public VDirId(string path, string appPath, long siteId)
        {
            if(path == null) {
                throw new ArgumentNullException("path");
            }
            if(appPath == null) {
                throw new ArgumentNullException("appPath");
            }

            this.Path = path;
            this.AppPath = appPath;
            this.SiteId = siteId;

            this.Uuid = Core.Utils.Uuid.Encode($"{this.Path}{DELIMITER}{this.AppPath}{DELIMITER}{this.SiteId}", PURPOSE);
        }
    }
}
