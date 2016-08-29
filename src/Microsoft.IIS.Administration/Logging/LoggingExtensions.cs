// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Logging
{
    using Core;
    using Extensions.Logging;
    using Extensions.DependencyInjection;
    using Serilog;
    using System.IO;
    using AspNetCore.Hosting;
    using Serilog.Events;

    public static class LoggingExtensions
    {
        public static IServiceCollection AddApiLogging(this IServiceCollection services)
        {
            var sp = services.BuildServiceProvider();
            var config = sp.GetRequiredService<Core.Config.IConfiguration>();
            var appBasePath = sp.GetRequiredService<IHostingEnvironment>().ContentRootPath;
            var defaultLogsRoot = Path.GetFullPath(Path.Combine(appBasePath, "logs"));

            var loggingConfiguration = config.Logging;
            var logsRoot = loggingConfiguration.LogsRoot;
            var minLevel = loggingConfiguration.MinLevel;

            // If invalid directory was specified in the configuration. Reset to default
            if (!Directory.Exists(logsRoot)) {
                logsRoot = defaultLogsRoot;
            }

            if (!loggingConfiguration.Enabled) {
                // Disable logging
                minLevel = (LogLevel)(1 + (int)LogEventLevel.Fatal);
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel
                .Is(loggingConfiguration.ToLogEventLevel(minLevel))
                .WriteTo
                .RollingFile(Path.Combine(logsRoot, loggingConfiguration.FileName), retainedFileCountLimit: null)
                .CreateLogger();

            services.AddSingleton(typeof(ILoggingConfiguration), (s) => { return loggingConfiguration; });

            ILoggerFactory loggerFactory = services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
            loggerFactory.AddSerilog();

            return services;
        }

        public static IServiceCollection AddApiAuditing(this IServiceCollection services)
        {
            var sp = services.BuildServiceProvider();
            var config = sp.GetRequiredService<Core.Config.IConfiguration>();
            var appBasePath = sp.GetRequiredService<IHostingEnvironment>().ContentRootPath;
            var defaultLogsRoot = Path.GetFullPath(Path.Combine(appBasePath, "logs"));

            var auditingConfiguration = config.Auditing;
            var logsRoot = auditingConfiguration.LogsRoot;
            var minLevel = auditingConfiguration.MinLevel;

            // If invalid directory was specified in the configuration. Reset to default
            if (!Directory.Exists(logsRoot)) {
                logsRoot = defaultLogsRoot;
            }

            if (!auditingConfiguration.Enabled) {
                // Disable auditing
                minLevel = (LogLevel)(1 + (int)LogEventLevel.Fatal);
            }

            AuditAttribute.Logger = new LoggerConfiguration()
                .MinimumLevel
                .Is(auditingConfiguration.ToLogEventLevel(minLevel))
                .WriteTo
                .RollingFile(Path.Combine(logsRoot, auditingConfiguration.FileName), retainedFileCountLimit: null)
                .CreateLogger();

            return services;
        }
    }
}
