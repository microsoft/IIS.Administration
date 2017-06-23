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
    using Extensions.Configuration;
    using System;

    public static class LoggingExtensions
    {
        public static IServiceCollection AddApiLogging(this IServiceCollection services) {
            IServiceProvider sp = services.BuildServiceProvider();

            var configuration = sp.GetRequiredService<IConfiguration>();
            var env = sp.GetRequiredService<IHostingEnvironment>();

            var loggingConfiguration = new LoggingConfiguration(configuration);
            var logsRoot = loggingConfiguration.LogsRoot;
            var minLevel = loggingConfiguration.MinLevel;
            var defaultLogsRoot = loggingConfiguration.GetDefaultLogRoot(env);

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
                .Is(LoggingConfiguration.ToLogEventLevel(minLevel))
                .WriteTo
                .RollingFile(Path.Combine(logsRoot, loggingConfiguration.FileName), retainedFileCountLimit: loggingConfiguration.MaxFiles)
                .CreateLogger();
            
            //
            // Wire up logging as soon as possible
            ILoggerFactory loggerFactory = services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
            loggerFactory.AddSerilog();

            return services;
        }

        public static IServiceCollection AddApiAuditing(this IServiceCollection services)
        {
            IServiceProvider sp = services.BuildServiceProvider();

            var configuration = sp.GetRequiredService<IConfiguration>();
            var env = sp.GetRequiredService<IHostingEnvironment>();

            var auditingConfiguration = new AuditingConfiguration(configuration);
            var auditRoot = auditingConfiguration.AuditingRoot;
            var minLevel = auditingConfiguration.MinLevel;
            var defaultAuditRoot = auditingConfiguration.GetDefaultAuditRoot(env);

            // If invalid directory was specified in the configuration. Reset to default
            if (!Directory.Exists(auditRoot)) {
                auditRoot = defaultAuditRoot;
            }

            if (!auditingConfiguration.Enabled) {
                // Disable auditing
                minLevel = (LogLevel)(1 + (int)LogEventLevel.Fatal);
            }

            AuditAttribute.Logger = new LoggerConfiguration()
                .MinimumLevel
                .Is(LoggingConfiguration.ToLogEventLevel(minLevel))
                .WriteTo
                .RollingFile(Path.Combine(auditRoot, auditingConfiguration.FileName), retainedFileCountLimit: auditingConfiguration.MaxFiles)
                .CreateLogger();

            services.AddSingleton<INonsensitiveAuditingFields>(new NonsensitiveAuditingFields());

            return services;
        }
    }
}
