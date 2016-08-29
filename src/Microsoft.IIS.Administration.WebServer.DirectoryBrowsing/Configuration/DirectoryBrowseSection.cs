// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.DirectoryBrowsing
{
using Microsoft.Web.Administration;

    public sealed class DirectoryBrowseSection : ConfigurationSection {

        private const string ShowFlagsAttribute = "showFlags";
        private const string EnabledAttribute = "enabled";

        public DirectoryBrowseSection() {
        }

        public bool Enabled {
            get {
                return (bool)base[EnabledAttribute];
            }
            set {
                base[EnabledAttribute] = value;
            }
        }

        public DirectoryBrowseShowFlags ShowFlags {
            get {
                return (DirectoryBrowseShowFlags)base[ShowFlagsAttribute];
            }
            set {
                base[ShowFlagsAttribute] = (int)value;
            }
        }
    }
}
