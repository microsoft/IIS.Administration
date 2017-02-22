// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRequestTracing
{
    using Files;
    using System;

    class TraceInfo
    {
        public IFileInfo File;
        public string Url;
        public string Method;
        public float StatusCode;
        public DateTime Date;
        public int TimeTaken;
        public string ProcessId;
        public string ActivityId;
    }
}
