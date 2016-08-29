// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Logging
{
    using Extensions.Logging;
    using Serilog.Events;

    public interface ILoggingConfiguration
    {
        bool Enabled { get; set; }
        string LogsRoot { get; set; }
        LogLevel MinLevel { get; set; }
        string FileName { get; set; }

        LogEventLevel ToLogEventLevel(LogLevel logLevel);
    }
}
