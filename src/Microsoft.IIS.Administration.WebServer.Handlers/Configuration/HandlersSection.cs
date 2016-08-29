// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Handlers
{
    using Microsoft.Web.Administration;

    public sealed class HandlersSection : ConfigurationSection {

        private const string AccessPolicyAttribute = "accessPolicy";

        private HandlerActionCollection _handlers;

        public HandlersSection() {
        }

        public HandlerAccessPolicy AccessPolicy {
            get {
                return (HandlerAccessPolicy)base[AccessPolicyAttribute];
            }
            set {
                base[AccessPolicyAttribute] = (int)value;
            }
        }

        public HandlerActionCollection Mappings {
            get {
                if (_handlers == null) {
                    _handlers = (HandlerActionCollection)GetCollection(typeof(HandlerActionCollection));
                }

                return _handlers;
            }
        }
    }
}
