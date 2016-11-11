// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Files
{
    using System;
    using Web.Administration;

    public class FullyQualifiedVirtualDirectory
    {
        public Site Site { get; private set; }
        public Application Application { get; private set; }
        public VirtualDirectory VirtualDirectory { get; private set; }

        public FullyQualifiedVirtualDirectory(Site site, Application app, VirtualDirectory vdir)
        {
            if (site == null) {
                throw new ArgumentNullException(nameof(site));
            }
            if (app == null) {
                throw new ArgumentNullException(nameof(app));
            }
            if (vdir == null) {
                throw new ArgumentNullException(nameof(vdir));
            }
            this.Site = site;
            this.Application = app;
            this.VirtualDirectory = vdir;
        }
    }
}
