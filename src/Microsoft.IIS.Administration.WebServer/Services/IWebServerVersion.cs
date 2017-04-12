// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    using System;

    public interface IWebServerVersion
    {
        /// <summary>
        /// Returns the version of IIS on the machine or null if the version cannot be determined.
        /// </summary>
        /// <returns>The version of IIS on the machine or null if the version cannot be determined.</returns>
        Version Version { get; }
    }
}
