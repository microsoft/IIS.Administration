// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Modules
{
    using Microsoft.Web.Administration;

    public sealed class GlobalModule : ConfigurationElement {

        private const string ImageAttribute = "image";
        private const string NameAttribute = "name";
        private const string PreConditionAttribute = "preCondition";

        public GlobalModule() {
        }

        public string Image {
            get {
                return (string)base[ImageAttribute];
            }
            set {
                base[ImageAttribute] = value;
            }
        }

        public string Name {
            get {
                return (string)base[NameAttribute];
            }
            set {
                base[NameAttribute] = value;
            }
        }

        public string PreCondition {
            get {
                return (string)base[PreConditionAttribute];
            }
            set {
                base[PreConditionAttribute] = value;
            }
        }
    }
}
