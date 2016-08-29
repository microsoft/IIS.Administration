// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRequestTracing
{ 
    public enum FailedRequestTracingVerbosity {
        General = 0,
        CriticalError = 1,
        Error = 2,
        Warning = 3,
        Information = 4,
        Verbose = 5,
    }
}
