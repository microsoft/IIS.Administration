// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core.Config
{
    using Cors;
    using Logging;
    using System;
    using System.Collections.Generic;

    public interface IConfiguration
    {
        Guid HostId { get; }

        string HostName { get; }

        IEnumerable<string> Administrators { get; }

        string SiteCreationRoot { get; }

        ILoggingConfiguration Logging { get; }

        ILoggingConfiguration Auditing { get; }

        ICorsConfiguration Cors { get; }
    }
}
