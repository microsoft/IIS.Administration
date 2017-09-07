// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Monitoring
{
    public interface IWebSiteSnapshot
    {
        string Name { get; }

        long Uptime { get; }

        long BytesRecvSec { get; set; }

        long BytesSentSec { get; set; }

        long TotalConnectionAttempts { get; set; }

        long ConnectionAttemptsSec { get; set; }

        long CurrentConnections { get; set; }

        long TotalRequestsSec { get; set; }

        long TotalRequests { get; set; }
    }
}
