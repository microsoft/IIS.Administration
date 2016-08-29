// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Authorization
{
    using System;

    public class RuleId
    {
        private const string PURPOSE = "WebServer.Authorization.Rule";
        private const char DELIMITER = '\n';

        private const uint USERS_INDEX = 0;
        private const uint ROLES_INDEX = 1;
        private const uint VERBS_INDEX = 2;
        private const uint PATH_INDEX = 3;
        private const uint SITE_ID_INDEX = 4;

        public string Users { get; private set; }
        public string Roles { get; private set; }
        public string Verbs { get; private set; }
        public string Path { get; private set; }
        public long? SiteId { get; private set; }
        public string Uuid { get; private set; }

        public RuleId(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) {
                throw new ArgumentNullException("uuid");
            }

            var info = Core.Utils.Uuid.Decode(uuid, PURPOSE).Split(DELIMITER);

            string users = info[USERS_INDEX];
            string roles = info[ROLES_INDEX];
            string verbs = info[VERBS_INDEX];
            string path = info[PATH_INDEX];
            string siteId = info[SITE_ID_INDEX];
            
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

            this.Users = users;
            this.Roles = roles;
            this.Verbs = verbs;
            this.Uuid = uuid;
        }

        public RuleId(long? siteId, string path, string users, string roles, string verbs)
        {
            if (users == null) {
                throw new ArgumentNullException("users");
            }

            if (roles == null) {
                throw new ArgumentNullException("roles");
            }

            if (verbs == null) {
                throw new ArgumentNullException("verbs");
            }

            if (!string.IsNullOrEmpty(path) && siteId == null) {
                throw new ArgumentNullException("siteId");
            }

            this.Users = users;
            this.Roles = roles;
            this.Verbs = verbs;
            this.Path = path;

            this.SiteId = siteId;

            this.Uuid = Core.Utils.Uuid.Encode(this.Users +
                                                    DELIMITER + this.Roles +
                                                    DELIMITER + this.Verbs +
                                                    DELIMITER + this.Path + 
                                                    DELIMITER + (this.SiteId == null ? "" : this.SiteId.Value.ToString()), PURPOSE);
        }
    }
}
