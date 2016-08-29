// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Modules
{
    using Microsoft.Web.Administration;

    public sealed class Module : ConfigurationElement {

        private const string NameAttribute = "name";
        private const string PreConditionAttribute = "preCondition";
        private const string TypeAttribute = "type";

        public Module() {
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

        public string Type {
            get {
                return (string)base[TypeAttribute];
            }
            set {
                base[TypeAttribute] = value;
            }
        }
    }
}
