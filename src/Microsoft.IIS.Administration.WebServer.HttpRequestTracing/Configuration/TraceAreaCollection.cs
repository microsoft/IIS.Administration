// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRequestTracing
{
    using Web.Administration;

    public sealed class TraceAreaCollection : ConfigurationElementCollectionBase<TraceArea>
    {
        public TraceArea Add(string provider) {
            TraceArea element = CreateElement();

            element.Provider = provider;

            return Add(element);
        }

        protected override TraceArea CreateNewElement(string elementTagName) {
            return new TraceArea();
        }
    }
}
