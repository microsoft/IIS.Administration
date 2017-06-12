// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    sealed class OutboundRulesCollection : RuleCollectionBase {

        protected override RuleElement CreateNewElement(string elementTagName) {
            return new OutboundRuleElement();
        }
    }
}

