// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Delegation
{
    using Sites;
    using System;
    using System.Text;
    using Web.Administration;
    public class SectionId
    {
        private const string PURPOSE = "WebServer.Delegation.Section";
        private const char DELIMITER = '\n';

        private const uint SECTION_PATH_INDEX = 0;
        private const uint PATH_INDEX = 1;
        private const uint SITE_ID_INDEX = 2;
        private const uint CONFIG_SCOPE_INDEX = 3;

        public string SectionPath { get; private set; }
        public string ConfigScope { get; private set; }
        public string Path { get; private set; }
        public long? SiteId { get; private set; }
        public string Uuid { get; private set; }

        public SectionId(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) {
                throw new ArgumentNullException("uuid");
            }

            var info = Core.Utils.Uuid.Decode(uuid, PURPOSE).Split(DELIMITER);

            string sectionPath = info[SECTION_PATH_INDEX];
            string siteId = info[SITE_ID_INDEX];
            string path = info[PATH_INDEX];

            if (!string.IsNullOrEmpty(path)) {
                if (string.IsNullOrEmpty(siteId)) {
                    throw new ArgumentNullException("siteId");
                }
                this.Path = path;
                this.SiteId = long.Parse(siteId);
            }
            if (!string.IsNullOrEmpty(siteId)) {
                this.SiteId = long.Parse(siteId);
            }

            string configScope = "";
            int configScopeSegments = int.Parse(info[CONFIG_SCOPE_INDEX]);

            if(configScopeSegments > 0) {
                if(this.SiteId == null) {
                    throw new ArgumentNullException("siteId");
                }

                Site site = SiteHelper.GetSite(this.SiteId.Value);
                if(site == null) {
                    throw new Exception();
                }

                StringBuilder sb = new StringBuilder();
                sb.Append(site.Name);

                // Path starts with '/'
                string[] segs = path.Split('/');
                for(int i = 1; i < configScopeSegments; i++) {
                    sb.Append('/');
                    sb.Append(segs[i]);
                }
                configScope = sb.ToString();
            }


            this.SectionPath = sectionPath;
            this.ConfigScope = configScope;
            this.Uuid = uuid;
        }

        public SectionId(long? siteId, string path, string sectionPath, string configScope)
        {
            if(configScope == null) {
                throw new ArgumentNullException("configScope");
            }

            this.SectionPath = sectionPath;
            this.SiteId = siteId;
            this.Path = path;
            this.ConfigScope = configScope;

            // 'configScope' is always (site name + path) or less
            // We encode the number of segments in configScope so we don't have to encode the whole path
            int configSegments = 0;
            if (configScope != string.Empty) {

                int start = 0;
                int index = 0;
                while (start < configScope.Length) {
                    index = configScope.IndexOf('/', start);
                    if(index != -1) {
                        configSegments++;
                        start = index + 1;
                    }
                    else {
                        configSegments++;
                        break;
                    }
                }
                
            }

            string encodableSiteId = (this.SiteId == null ? "" : this.SiteId.Value.ToString());

            this.Uuid = Core.Utils.Uuid.Encode($"{this.SectionPath}{DELIMITER}{this.Path}{DELIMITER}{encodableSiteId}{DELIMITER}{configSegments.ToString()}", PURPOSE);
        }
    }
}
