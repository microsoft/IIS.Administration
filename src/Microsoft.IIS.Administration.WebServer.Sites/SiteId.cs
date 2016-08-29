// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Sites
{
    using System;

    public class SiteId
    {
        private const string PURPOSE = "Webserver.Site";

        public long Id { get; private set; }

        public string Uuid { get; private set; }

        public SiteId(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) {
                throw new ArgumentNullException(uuid);
            }

            this.Id = long.Parse(Core.Utils.Uuid.Decode(uuid, PURPOSE));
            this.Uuid = uuid;
        }

        public SiteId(long id)
        {
            this.Id = id;
            this.Uuid = Core.Utils.Uuid.Encode($"{Id}", PURPOSE);
        }
    }
}
