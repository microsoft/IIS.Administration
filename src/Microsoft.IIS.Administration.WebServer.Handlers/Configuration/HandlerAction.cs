// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Handlers
{
    using Microsoft.Web.Administration;

    public sealed class Mapping : ConfigurationElement {

        private const string ModulesAttribute = "modules";
        private const string NameAttribute = "name";
        private const string PathAttribute = "path";
        private const string PreConditionAttribute = "preCondition";
        private const string RequireAccessAttribute = "requireAccess";
        private const string ResourceTypeAttribute = "resourceType";
        private const string ScriptProcessorAttribute = "scriptProcessor";
        private const string TypeAttribute = "type";
        private const string VerbAttribute = "verb";
        private const string AllowPathInfoAttribute = "allowPathInfo";
        private const string ResponseBufferLimitAttribute = "responseBufferLimit";

        public Mapping() {
        }

        public bool AllowPathInfo {
            get {
                return (bool)base[AllowPathInfoAttribute];
            }
            set {
                base[AllowPathInfoAttribute] = value;
            }
        }

        public string Modules {
            get {
                return (string)base[ModulesAttribute];
            }
            set {
                base[ModulesAttribute] = value;
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

        public string Path {
            get {
                return (string)base[PathAttribute];
            }
            set {
                base[PathAttribute] = value;
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

        public HandlerRequiredAccess RequireAccess {
            get {
                return (HandlerRequiredAccess)base[RequireAccessAttribute];
            }
            set {
                base[RequireAccessAttribute] = (int)value;
            }
        }

        public ResourceType ResourceType {
            get {
                return (ResourceType)base[ResourceTypeAttribute];
            }
            set {
                base[ResourceTypeAttribute] = (int)value;
            }
        }

        public long ResponseBufferLimit {
            get { 
                return (long)base[ResponseBufferLimitAttribute];
            }
            set {
                base[ResponseBufferLimitAttribute] = value;
            }
        }

        public string ScriptProcessor {
            get {
                return (string)base[ScriptProcessorAttribute];
            }
            set {
                base[ScriptProcessorAttribute] = value;
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

        public string Verb {
            get {
                return (string)base[VerbAttribute];
            }
            set {
                base[VerbAttribute] = value;
            }
        }

    }
}
