// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Logging
{
    using Extensions.Logging;
    using Serilog.Events;


    class LoggingConfiguration : ILoggingConfiguration
    {
        public bool Enabled { get; set; }
        public string LogsRoot { get; set; }
        public LogLevel MinLevel { get; set; }
        public string FileName { get; set; }

        public LogEventLevel ToLogEventLevel(LogLevel logLevel)
        {
            switch (logLevel) {
                case LogLevel.Critical:
                    return LogEventLevel.Fatal;
                case LogLevel.Error:
                    return LogEventLevel.Error;
                case LogLevel.Warning:
                    return LogEventLevel.Warning;
                case LogLevel.Information:
                    return LogEventLevel.Information;
                case LogLevel.Debug:
                    return LogEventLevel.Debug;
                case LogLevel.Trace:
                    return LogEventLevel.Verbose;
                default:
                    return (LogEventLevel.Fatal + 1);
            }
        }
    }
}
