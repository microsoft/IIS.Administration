// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    sealed class InboundRuleCollection : RuleCollectionBase {

        protected override void CopyInfo(RuleElement source, RuleElement destination) {
            base.CopyInfo(source, destination);
            InboundRule sourceRuleElement = (InboundRule)source;
            InboundRule destinationRuleElement = (InboundRule)destination;

            sourceRuleElement.ServerVariableAssignments.CopyTo(destinationRuleElement.ServerVariableAssignments);
        }

        protected override RuleElement CreateNewElement(string elementTagName) {
            return new InboundRule();
        }
    }
}

