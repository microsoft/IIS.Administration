// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Files
{
    using System;
    using Web.Administration;

    class Vdir
    {
        public Site Site { get; private set; }
        public Application Application { get; private set; }
        public VirtualDirectory VirtualDirectory { get; private set; }

        public string Path {
            get {
                var p = Application.Path.TrimEnd('/') + VirtualDirectory.Path.TrimEnd('/');
                return p == string.Empty ? "/" : p;
            }
        }

        public string Name {
            get {
                bool isRootApp = Application.Path == "/";
                bool isRootVdir = VirtualDirectory.Path == "/";
                if (isRootApp && isRootVdir) {
                    return Site.Name;
                }
                else if (isRootVdir) {
                    return Application.Path.TrimStart('/');
                }
                else {
                    var parts = Path.Split('/');
                    return parts[parts.Length - 1];
                }
            }
        }

        public Vdir(Site site, Application app, VirtualDirectory vdir)
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

        public static FileType GetVdirType(VirtualDirectory virtualDirectory)
        {
            return virtualDirectory.Path == "/" ? FileType.Application : FileType.VDir;
        }
    }
}
