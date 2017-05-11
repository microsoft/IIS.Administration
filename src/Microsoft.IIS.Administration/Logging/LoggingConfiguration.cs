// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Logging
{
    using AspNetCore.Hosting;
    using Extensions.Configuration;
    using Extensions.Logging;
    using Serilog.Events;
    using System;
    using System.IO;

    class LoggingConfiguration
    {
        public bool Enabled { get; set; }
        public string LogsRoot { get; set; }
        public LogLevel MinLevel { get; set; }
        public string FileName { get; set; }
        public int MaxFiles { get; set; }

        public LoggingConfiguration(IConfiguration configuration)
        {
            Enabled = configuration.GetValue("logging:enabled", true);
            LogsRoot = Environment.ExpandEnvironmentVariables(configuration.GetValue("logging:path", string.Empty));
            MinLevel = configuration.GetValue("logging:min_level", LogLevel.Error);
            FileName = configuration.GetValue("logging:file_name", "log-{Date}.txt");
            MaxFiles = configuration.GetValue("logging:max_files", 50);
        }

        public static LogEventLevel ToLogEventLevel(LogLevel logLevel)
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

        public string GetDefaultLogRoot(IHostingEnvironment env)
        {
            return Path.GetFullPath(Path.Combine(env.ContentRootPath, "../../logs"));
        }
    }
}
