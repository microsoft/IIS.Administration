// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRequestTracing
{
    internal static class FailureTracingGlobals
    {
        public const string FailureTracingSectionName = "system.webServer/tracing/traceFailedRequests";
        public const string ProviderDefinitionsSectionName = "system.webServer/tracing/traceProviderDefinitions";
    }
}
