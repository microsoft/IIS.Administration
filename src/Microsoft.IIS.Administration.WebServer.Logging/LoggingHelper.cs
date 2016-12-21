// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Logging
{
    using System;
    using Web.Administration;
    using System.Collections.Generic;
    using Core;
    using System.Linq;
    using Core.Utils;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json;
    using Sites;
    using System.IO;
    using System.Dynamic;
    using Files;

    public static class LoggingHelper
    {
        private const string LogTargetW3CAttribute = "logTargetW3C";
        private const string CustomFieldsElement = "customFields";

        internal static object ToJsonModel(Site site, string path)
        {
            LogSection logSection = GetLogSection(site, path);
            HttpLoggingSection httpLogSection = GetHttpLoggingSection(site, path);

            SiteLogFile logFile = null;
            if (site == null) {
                logFile = ManagementUnit.Current.ServerManager.SiteDefaults.LogFile;
            }
            else {
                logFile = site.LogFile;
            }

            LoggingId id = new LoggingId(site?.Id, path, httpLogSection.IsLocallyStored);

            Dictionary<string, bool> logTargetW3C = new Dictionary<string, bool>();
            if (logFile.Schema.HasAttribute(LogTargetW3CAttribute)) {
                LogTargetW3C target = logFile.LogTargetW3C;
                
                logTargetW3C.Add("etw", target.HasFlag(LogTargetW3C.ETW));
                logTargetW3C.Add("file", target.HasFlag(LogTargetW3C.File));
            }

            FileLogFormat logFormat;

            if (logSection.CentralLogFileMode == CentralLogFileMode.Site) {
                logFormat = FromLogFormat(logFile.LogFormat);
            }
            else {
                logFormat = logSection.CentralLogFileMode == CentralLogFileMode.CentralBinary ? FileLogFormat.Binary : FileLogFormat.W3c;
            }

            
            bool logFilePerSite = logSection.CentralLogFileMode == CentralLogFileMode.Site;

            string directory = default(string);
            string period = default(string);
            long truncateSize = default(long);
            bool useLocalTime = default(bool);
            Dictionary<string, bool> logTargetW3c = default(Dictionary<string, bool>);
            Dictionary<string, bool> logFields = default(Dictionary<string, bool>);
            IEnumerable<object> customLogFields = default(IEnumerable<object>);

            switch(logSection.CentralLogFileMode) {
                case CentralLogFileMode.CentralBinary:

                    directory = logSection.CentralBinaryLogFile.Directory;
                    period = PeriodRepresentation(logSection.CentralBinaryLogFile.Period);
                    truncateSize = logSection.CentralBinaryLogFile.TruncateSize;
                    useLocalTime = logSection.CentralBinaryLogFile.LocalTimeRollover;
                    logTargetW3c = null;
                    logFields = null;
                    customLogFields = null;
                    break;

                case CentralLogFileMode.CentralW3C:

                    directory = logSection.CentralW3CLogFile.Directory;
                    period = PeriodRepresentation(logSection.CentralW3CLogFile.Period);
                    truncateSize = logSection.CentralW3CLogFile.TruncateSize;
                    useLocalTime = logSection.CentralW3CLogFile.LocalTimeRollover;
                    logTargetW3c = null;
                    logFields = LogExtFileFlagsRepresentation(logSection.CentralW3CLogFile.LogExtFileFlags);
                    customLogFields = null;

                    break;
                case CentralLogFileMode.Site:

                    directory = logFile.Directory;
                    period = PeriodRepresentation(logFile.Period);
                    truncateSize = logFile.TruncateSize;
                    useLocalTime = logFile.LocalTimeRollover;
                    logTargetW3c = logTargetW3C;
                    logFields = LogExtFileFlagsRepresentation(logFile.LogExtFileFlags);

                    if (logFile.Schema.HasChildElement(CustomFieldsElement)) {
                        customLogFields = logFile.CustomLogFields.Select(custField => {
                            return new
                            {
                                field_name = custField.LogFieldName,
                                source_name = custField.SourceName,
                                source_type = SourceTypeRepresentation(custField.SourceType)
                            };
                        });
                    }

                    break;
            }

            dynamic o = new ExpandoObject();

            o.id = id.Uuid;
            o.scope = site == null ? string.Empty : site.Name + path;

            // The metadata is obtained solely from <httpLogSection> because the <log> section is in applicationHost/* path which means it can't be accessed in web configs
            o.metadata = ConfigurationUtility.MetadataToJson(httpLogSection.IsLocallyStored, httpLogSection.IsLocked, httpLogSection.OverrideMode, httpLogSection.OverrideModeEffective);

            o.enabled = IsLoggingEnabled(httpLogSection, logSection, logFile);
            o.log_per_site = logFilePerSite;

            o.directory = directory;
            o.log_file_encoding = logSection.LogInUTF8 ? "utf-8" : "ansi";
            o.log_file_format = Enum.GetName(typeof(FileLogFormat), logFormat).ToLower();

            if (logFile.Schema.HasAttribute(LogTargetW3CAttribute)) {
                o.log_target = logTargetW3c;
            }

            o.log_fields = logFields;

            if (logFile.Schema.HasChildElement(CustomFieldsElement)) {
                o.custom_log_fields = customLogFields;
            }

            o.rollover = new {
                period = period,
                truncate_size = truncateSize,
                use_local_time = useLocalTime,
            };

            o.website = SiteHelper.ToJsonModelRef(site);

            return Core.Environment.Hal.Apply(Defines.Resource.Guid, o);
        }

        public static void Update(dynamic model, Site site, string path, string configScope = null)
        {
            if(model == null) {
                throw new ApiArgumentException("model");
            }
            
            // Cannot configure at any path below site
            if(site != null && path != "/")
            {
                throw new InvalidScopeTypeException(string.Format("{0}{1}", (site == null ? "" : site.Name), path ?? ""));
            }

            LogSection logSection = GetLogSection(site, path, configScope);
            HttpLoggingSection httpLoggingSection = GetHttpLoggingSection(site, path, configScope);

            // Site log settings are set in the site defaults if Configuration target is server
            // If target is a site then the site's logfile element is used
            SiteLogFile siteLogFile = site == null ? ManagementUnit.Current.ServerManager.SiteDefaults.LogFile : site.LogFile;

            try {
                
                bool? enabled = DynamicHelper.To<bool>(model.enabled);
                if(enabled != null) {
                    TrySetLoggingEnabled(enabled.Value, httpLoggingSection, logSection, siteLogFile, site, configScope);
                }

                // Extract rollover from model
                dynamic rollover = model.rollover;
                if (rollover != null && !(rollover is JObject)) {
                    throw new ApiArgumentException("rollover", ApiArgumentException.EXPECTED_OBJECT);
                }

                // Only accessible at server level
                if (site == null) {
                    
                    DynamicHelper.If<bool>((object)model.log_per_site, v => logSection.CentralLogFileMode = v ? CentralLogFileMode.Site : CentralLogFileMode.CentralW3C);
                    DynamicHelper.If<FileLogFormat>((object)model.log_file_format, v => {
                        // Site log mode exposes 4 possible log formats
                        if(logSection.CentralLogFileMode == CentralLogFileMode.Site) {
                            switch(v) {
                                case FileLogFormat.Custom:
                                    siteLogFile.LogFormat = LogFormat.Custom;
                                    break;
                                case FileLogFormat.Iis:
                                    siteLogFile.LogFormat = LogFormat.Iis;
                                    break;
                                case FileLogFormat.W3c:
                                    siteLogFile.LogFormat = LogFormat.W3c;
                                    break;
                                case FileLogFormat.Ncsa:
                                    siteLogFile.LogFormat = LogFormat.Ncsa;
                                    break;
                                default:
                                    throw new ApiArgumentException("log_file_format");
                            }
                        }
                        // Server log mode exposes 2 possible log formats
                        else {
                            switch (v) {
                                case FileLogFormat.W3c:
                                    logSection.CentralLogFileMode = CentralLogFileMode.CentralW3C;
                                    break;
                                case FileLogFormat.Binary:
                                    logSection.CentralLogFileMode = CentralLogFileMode.CentralBinary;
                                    break;
                                default:
                                    throw new ApiArgumentException("log_file_format");
                            }
                        }
                    });
                    DynamicHelper.If((object)model.log_file_encoding, v => {
                        switch(v.ToLower()) {
                            case "utf-8":
                                logSection.LogInUTF8 = true;
                                break;
                            case "ansi":
                                logSection.LogInUTF8 = false;
                                break;
                            default:
                                throw new ApiArgumentException("log_file_encoding");
                        }
                    });

                    // Binary log mode settings
                    if (logSection.CentralLogFileMode == CentralLogFileMode.CentralBinary) {
                        dynamic bSettings = model;


                        CentralBinaryLogFile bFile = logSection.CentralBinaryLogFile;
                        
                        DynamicHelper.If((object)bSettings.directory, v => {
                            EnsureCanUseDirectory(ref v);
                            bFile.Directory = v;
                        });
                        
                        if(rollover != null) {
                            DynamicHelper.If<LoggingRolloverPeriod>((object)PeriodFromRepresentation(rollover.period), v => bFile.Period = v);
                            DynamicHelper.If((object)rollover.truncate_size, 1048576, 4294967295, v => bFile.TruncateSize = v);
                            DynamicHelper.If<bool>((object)rollover.use_local_time, v => bFile.LocalTimeRollover = v);
                        }
                    }

                    // W3C log mode settings
                    if (logSection.CentralLogFileMode == CentralLogFileMode.CentralW3C) {
                        dynamic wSettings = model;

                        CentralW3CLogFile wFile = logSection.CentralW3CLogFile;
                        
                        DynamicHelper.If((object)wSettings.directory, v => {
                            EnsureCanUseDirectory(ref v);
                            wFile.Directory = v;
                        });

                        if(rollover != null) {
                            DynamicHelper.If<LoggingRolloverPeriod>((object)PeriodFromRepresentation(rollover.period), v => wFile.Period = v);
                            DynamicHelper.If((object)rollover.truncate_size, 1048576, 4294967295, v => wFile.TruncateSize = v);
                            DynamicHelper.If<bool>((object)rollover.use_local_time, v => wFile.LocalTimeRollover = v);
                        }

                        if (wSettings.log_fields != null) {

                            try {
                                wFile.LogExtFileFlags = SetLogFieldFlags(wFile.LogExtFileFlags, wSettings.log_fields);
                            }
                            catch (ApiArgumentException e) {
                                throw new ApiArgumentException("w3c_settings.log_fields", e);
                            }
                            catch (JsonSerializationException e) {
                                throw new ApiArgumentException("w3c_settings.log_fields", e);
                            }
                        }

                    }
                }

                //
                // Per site mode format
                DynamicHelper.If<FileLogFormat>((object)model.log_file_format, v => {
                    if (logSection.CentralLogFileMode == CentralLogFileMode.Site) {
                        switch (v) {
                            case FileLogFormat.Custom:
                                siteLogFile.LogFormat = LogFormat.Custom;
                                break;
                            case FileLogFormat.Iis:
                                siteLogFile.LogFormat = LogFormat.Iis;
                                break;
                            case FileLogFormat.W3c:
                                siteLogFile.LogFormat = LogFormat.W3c;
                                break;
                            case FileLogFormat.Ncsa:
                                siteLogFile.LogFormat = LogFormat.Ncsa;
                                break;
                            default:
                                throw new ApiArgumentException("log_file_format");
                        }
                    }
                });

                // Site settings
                if (logSection.CentralLogFileMode == CentralLogFileMode.Site) {
                    dynamic siteSettings = model;
                    
                    DynamicHelper.If((object)siteSettings.directory, v => {
                        EnsureCanUseDirectory(ref v);
                        siteLogFile.Directory = v;
                    });

                    if(rollover != null) {
                        DynamicHelper.If<LoggingRolloverPeriod>((object)PeriodFromRepresentation(rollover.period), v => siteLogFile.Period = v);
                        DynamicHelper.If((object)rollover.truncate_size, 1048576, 4294967295, v => siteLogFile.TruncateSize = v);
                        DynamicHelper.If<bool>((object)rollover.use_local_time, v => siteLogFile.LocalTimeRollover = v);
                    }

                    if (siteSettings.log_fields != null) {

                        try {
                            siteLogFile.LogExtFileFlags = SetLogFieldFlags(siteLogFile.LogExtFileFlags, siteSettings.log_fields);
                        }
                        catch (ApiArgumentException e) {
                            throw new ApiArgumentException("site_settings.log_fields", e);
                        }
                        catch (JsonSerializationException e) {
                            throw new ApiArgumentException("site_settings.log_fields", e);
                        }
                    }

                    if (siteSettings.log_target != null && siteLogFile.Schema.HasAttribute(LogTargetW3CAttribute)) {

                        try {
                            Dictionary<string, bool> logTargets = JsonConvert.DeserializeObject<Dictionary<string, bool>>(siteSettings.log_target.ToString());

                            if (logTargets == null) {
                                throw new ApiArgumentException("site_settings.log_target_w3c");
                            }

                            LogTargetW3C logTargetW3C = siteLogFile.LogTargetW3C;

                            if (logTargets.ContainsKey("etw")) {
                                if(logTargets["etw"]) {
                                    logTargetW3C |= LogTargetW3C.ETW;
                                }
                                else {
                                    logTargetW3C &= ~LogTargetW3C.ETW;
                                }
                            }
                            if (logTargets.ContainsKey("file")) {
                                if (logTargets["file"]) {
                                    logTargetW3C |= LogTargetW3C.File;
                                }
                                else {
                                    logTargetW3C &= ~LogTargetW3C.File;
                                }
                            }

                            siteLogFile.LogTargetW3C = logTargetW3C;
                        }
                        catch (JsonSerializationException e) {
                            throw new ApiArgumentException("site_settings.log_fields", e);
                        }
                    }
                    
                    if (siteSettings.custom_log_fields != null && siteLogFile.Schema.HasChildElement(CustomFieldsElement)) {
                        IEnumerable<dynamic> customFields = siteSettings.custom_log_fields;

                        List<CustomField> tempCustFields = new List<CustomField>();

                        foreach (dynamic field in customFields) {

                            string fieldName = DynamicHelper.Value(field.field_name);
                            string sourceName = DynamicHelper.Value(field.source_name);
                            CustomLogFieldSourceType? sourceType = SourceTypeFromRepresentation(field.source_type);

                            if (string.IsNullOrEmpty(fieldName)) {
                                throw new ApiArgumentException("custom_log_field.field_name");
                            }
                            if (string.IsNullOrEmpty(sourceName)) {
                                throw new ApiArgumentException("custom_log_field.source_name");
                            }
                            if (sourceType == null) {
                                throw new ApiArgumentException("custom_log_field.source_type");
                            }

                            tempCustFields.Add(new CustomField() {
                                FieldName = fieldName,
                                SourceName = sourceName,
                                SourceType = sourceType.Value
                            });
                        }

                        siteLogFile.CustomLogFields.Clear();
                        tempCustFields.ForEach(f => siteLogFile.CustomLogFields.Add(f.FieldName, f.SourceName, f.SourceType));
                    }
                }


                if (model.metadata != null) {

                    DynamicHelper.If<OverrideMode>((object)model.metadata.override_mode, v => httpLoggingSection.OverrideMode = v);
                }

            }
            catch (FileLoadException e) {
                throw new LockedException(logSection.SectionPath + "|" + httpLoggingSection.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }

        public static FileLogFormat FromLogFormat(LogFormat format)
        {
            switch (format) {
                case LogFormat.Custom:
                    return FileLogFormat.Custom;
                case LogFormat.Iis:
                    return FileLogFormat.Iis;
                case LogFormat.Ncsa:
                    return FileLogFormat.Ncsa;
                case LogFormat.W3c:
                    return FileLogFormat.W3c;
                default:
                    throw new ArgumentException(nameof(format));
            }
        }



        private static bool IsLoggingEnabled(HttpLoggingSection httpLoggingSection, LogSection logSection, SiteLogFile siteLogFile)
        {
            CentralLogFileMode mode = logSection.CentralLogFileMode;

            bool modeEnabled = false;
            switch(mode) {
                case CentralLogFileMode.CentralBinary:
                    modeEnabled = logSection.CentralBinaryLogFile.Enabled;
                    break;
                case CentralLogFileMode.CentralW3C:
                    modeEnabled = logSection.CentralW3CLogFile.Enabled;
                    break;
                case CentralLogFileMode.Site:
                    modeEnabled = siteLogFile.Enabled;
                    break;
                default:
                    break;
            }

            return modeEnabled && !httpLoggingSection.DontLog;
        }

        private static void TrySetLoggingEnabled(bool value, HttpLoggingSection httpLoggingSection, LogSection logSection, SiteLogFile siteLogFile, Site site, string configScope = null)
        {
            //
            // Logging is configurable only at server and site level

            if(site == null) {
                httpLoggingSection.DontLog = !value;

                logSection.CentralW3CLogFile.Enabled = value;
                logSection.CentralBinaryLogFile.Enabled = value;
            }
            else {
                if (httpLoggingSection.IsLocked && (configScope == null || configScope != ManagementUnit.APP_HOST_CONFIG_SCOPE)) {
                    if (value && httpLoggingSection.DontLog) {
                        throw new LockedException("enabled");
                    }
                }
                else {
                    httpLoggingSection.DontLog = !value;
                }
                
                siteLogFile.Enabled = value;
            }
        }
        
        private static LogSection GetLogSection(Site site, string path, string configScope = null)
        {

            return (LogSection)ManagementUnit.GetConfigSection(site?.Id,
                                                                        path,
                                                                        LoggingGlobals.WwwServiceLoggingSectionName,
                                                                        typeof(LogSection),
                                                                        configScope);
        }

        public static HttpLoggingSection GetHttpLoggingSection(Site site, string path, string configScope = null)
        {

            return (HttpLoggingSection)ManagementUnit.GetConfigSection(site?.Id,
                                                                        path,
                                                                        LoggingGlobals.HttpLoggingSectionName,
                                                                        typeof(HttpLoggingSection),
                                                                        configScope);
        }

        public static bool IsSectionLocal(Site site, string path)
        {
            return ManagementUnit.IsSectionLocal(site?.Id,
                                                 path,
                                                 LoggingGlobals.HttpLoggingSectionName);
        }

        public static string GetLocation(string id)
        {
            return $"/{Defines.PATH}/{id}";
        }



        private static void EnsureCanUseDirectory(ref string path)
        {
            path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            var expanded = System.Environment.ExpandEnvironmentVariables(path);

            if (!PathUtil.IsFullPath(expanded)) {
                throw new ApiArgumentException("directory");
            }
            if (!FileProvider.Default.IsAccessAllowed(expanded, FileAccess.Read)) {
                throw new ForbiddenArgumentException("directory", expanded);
            }
        }

        private class CustomField {
            public string FieldName { get; set; }
            public string SourceName { get; set; }
            public CustomLogFieldSourceType SourceType {get; set;}
        }

        private static string SourceTypeRepresentation(CustomLogFieldSourceType sourceType)
        {
            switch (sourceType) {
                case CustomLogFieldSourceType.RequestHeader:
                    return "request_header";
                case CustomLogFieldSourceType.ResponseHeader:
                    return "response_header";
                case CustomLogFieldSourceType.ServerVariable:
                    return "server_variable";
                default:
                    return null;
            }
        }

        private static CustomLogFieldSourceType? SourceTypeFromRepresentation(dynamic sourceType)
        {
            string t = DynamicHelper.Value(sourceType);

            if (t == null) {
                return null;
            }

            switch (t) {
                case "request_header":
                    return CustomLogFieldSourceType.RequestHeader;
                case "response_header":
                    return CustomLogFieldSourceType.ResponseHeader;
                case "server_variable":
                    return CustomLogFieldSourceType.ServerVariable;
                default:
                    return null;
            }
        }

        private static string PeriodRepresentation(LoggingRolloverPeriod period)
        {
            switch (period) {
                case LoggingRolloverPeriod.MaxSize:
                    return "max_size";
                default:
                    return Enum.GetName(typeof(LoggingRolloverPeriod), period).ToLower();
            }
        }

        private static LoggingRolloverPeriod? PeriodFromRepresentation(dynamic period)
        {
            string p = DynamicHelper.Value(period);

            if(string.IsNullOrEmpty(p)) {
                return null;
            }

            if (p.Equals("max_size")) {
                return LoggingRolloverPeriod.MaxSize;
            }

            return DynamicHelper.To<LoggingRolloverPeriod>(period);
        }

        private static Dictionary<string, bool> LogExtFileFlagsRepresentation(LogExtFileFlags flags)
        {
            Dictionary<string, bool> logExtFileFlags = new Dictionary<string, bool>();
            logExtFileFlags.Add("date", flags.HasFlag(LogExtFileFlags.Date));
            logExtFileFlags.Add("time", flags.HasFlag(LogExtFileFlags.Time));
            logExtFileFlags.Add("client_ip", flags.HasFlag(LogExtFileFlags.ClientIP));
            logExtFileFlags.Add("username", flags.HasFlag(LogExtFileFlags.UserName));
            logExtFileFlags.Add("site_name", flags.HasFlag(LogExtFileFlags.SiteName));
            logExtFileFlags.Add("computer_name", flags.HasFlag(LogExtFileFlags.ComputerName));
            logExtFileFlags.Add("server_ip", flags.HasFlag(LogExtFileFlags.ServerIP));
            logExtFileFlags.Add("method", flags.HasFlag(LogExtFileFlags.Method));
            logExtFileFlags.Add("uri_stem", flags.HasFlag(LogExtFileFlags.UriStem));
            logExtFileFlags.Add("uri_query", flags.HasFlag(LogExtFileFlags.UriQuery));
            logExtFileFlags.Add("http_status", flags.HasFlag(LogExtFileFlags.HttpStatus));
            logExtFileFlags.Add("win_32_status", flags.HasFlag(LogExtFileFlags.Win32Status));
            logExtFileFlags.Add("bytes_sent", flags.HasFlag(LogExtFileFlags.BytesSent));
            logExtFileFlags.Add("bytes_recv", flags.HasFlag(LogExtFileFlags.BytesRecv));
            logExtFileFlags.Add("time_taken", flags.HasFlag(LogExtFileFlags.TimeTaken));
            logExtFileFlags.Add("server_port", flags.HasFlag(LogExtFileFlags.ServerPort));
            logExtFileFlags.Add("user_agent", flags.HasFlag(LogExtFileFlags.UserAgent));
            logExtFileFlags.Add("cookie", flags.HasFlag(LogExtFileFlags.Cookie));
            logExtFileFlags.Add("referer", flags.HasFlag(LogExtFileFlags.Referer));
            logExtFileFlags.Add("protocol_version", flags.HasFlag(LogExtFileFlags.ProtocolVersion));
            logExtFileFlags.Add("host", flags.HasFlag(LogExtFileFlags.Host));
            logExtFileFlags.Add("http_sub_status", flags.HasFlag(LogExtFileFlags.HttpSubStatus));

            return logExtFileFlags;
        }

        // throws JsonSerializationException, ApiArgumentException
        private static LogExtFileFlags SetLogFieldFlags(LogExtFileFlags flags, dynamic model)
        {
            Dictionary<string, bool> logFields = JsonConvert.DeserializeObject<Dictionary<string, bool>>(model.ToString());

            if (logFields == null) {
                throw new ApiArgumentException(string.Empty);
            }

            Dictionary<string, LogExtFileFlags> flagPairs = new Dictionary<string, LogExtFileFlags>
            {
                { "date", LogExtFileFlags.Date },
                { "time", LogExtFileFlags.Time },
                { "client_ip", LogExtFileFlags.ClientIP },
                { "username", LogExtFileFlags.UserName },
                { "site_name", LogExtFileFlags.SiteName },
                { "computer_name", LogExtFileFlags.ComputerName },
                { "server_ip", LogExtFileFlags.ServerIP },
                { "method", LogExtFileFlags.Method },
                { "uri_stem", LogExtFileFlags.UriStem },
                { "uri_query", LogExtFileFlags.UriQuery },
                { "http_status", LogExtFileFlags.HttpStatus },
                { "win_32_status", LogExtFileFlags.Win32Status },
                { "bytes_sent", LogExtFileFlags.BytesSent },
                { "bytes_recv", LogExtFileFlags.BytesRecv },
                { "time_taken", LogExtFileFlags.TimeTaken },
                { "server_port", LogExtFileFlags.ServerPort },
                { "user_agent", LogExtFileFlags.UserAgent },
                { "cookie", LogExtFileFlags.Cookie },
                { "referer", LogExtFileFlags.Referer },
                { "protocol_version", LogExtFileFlags.ProtocolVersion },
                { "host", LogExtFileFlags.Host },
                { "http_sub_status", LogExtFileFlags.HttpSubStatus },

            };

            foreach(var key in flagPairs.Keys)
            {
                if (logFields.ContainsKey(key))
                {
                    if (logFields[key])
                    {
                        flags |= flagPairs[key];
                    }
                    else {
                        flags &= ~flagPairs[key];
                    }
                }
            }

            return flags;
        }
    }
}
