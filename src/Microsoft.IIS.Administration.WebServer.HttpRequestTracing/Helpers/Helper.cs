// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRequestTracing
{
    using Core;
    using Core.Utils;
    using Files;
    using Sites;
    using System.IO;
    using Web.Administration;

    public static class Helper
    {
        public static bool IsSectionLocal(Site site, string path)
        {
            return GetTraceFailedRequestsSection(site, path).IsLocallyStored;
        }

        internal static object ToJsonModel(Site site, string path)
        {
            var section = GetTraceFailedRequestsSection(site, path);
            var providersSection = GetTraceProviderDefinitionSection(site, path);
            
            bool isLocal = section.IsLocallyStored;
            bool isLocked = section.IsLocked;
            OverrideMode overrideMode = section.OverrideMode;
            OverrideMode overrideModeEffective = section.OverrideModeEffective;

            HttpRequestTracingId hrtId = new HttpRequestTracingId(site?.Id, path, isLocal);

            bool enabled;
            string directory = null;
            long maximumNumberTraceFiles;
            
            if(site != null) {
                enabled = site.TraceFailedRequestsLogging.Enabled;
                directory = site.TraceFailedRequestsLogging.Directory;
                maximumNumberTraceFiles = site.TraceFailedRequestsLogging.MaxLogFiles;
            }
            else {
                var siteDefaults = ManagementUnit.ServerManager.SiteDefaults;
                enabled = true;
                directory = siteDefaults.TraceFailedRequestsLogging.Directory;
                maximumNumberTraceFiles = siteDefaults.TraceFailedRequestsLogging.MaxLogFiles;
            }

            var obj = new {
                id = hrtId.Uuid,
                scope = site == null ? string.Empty : site.Name + path,
                metadata = ConfigurationUtility.MetadataToJson(isLocal, isLocked, overrideMode, overrideModeEffective),
                enabled = enabled,
                directory = directory,
                maximum_number_trace_files = maximumNumberTraceFiles,
                website = SiteHelper.ToJsonModelRef(site)
            };

            return Environment.Hal.Apply(Defines.Resource.Guid, obj);
        }

        public static object ToJsonModelRef(Site site, string path)
        {
            var section = GetTraceFailedRequestsSection(site, path);

            HttpRequestTracingId hrtId = new HttpRequestTracingId(site?.Id, path, section.IsLocallyStored);

            var obj = new {
                id = hrtId.Uuid,
                scope = site == null ? string.Empty : site.Name + path,
            };

            return Environment.Hal.Apply(Defines.Resource.Guid, obj, false);
        }

        public static void UpdateSettings(dynamic model, Site site, string path, string configPath = null)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            // Only editable at site level
            if (site != null && path == "/") {

                    DynamicHelper.If((object)model.directory, v => {

                        v = v.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                        var expanded = System.Environment.ExpandEnvironmentVariables(v);

                        if (!PathUtil.IsFullPath(expanded)) {
                            throw new ApiArgumentException("directory");
                        }
                        if (!FileProvider.Default.IsAccessAllowed(expanded, FileAccess.Read)) {
                            throw new ForbiddenArgumentException("directory", expanded);
                        }

                        site.TraceFailedRequestsLogging.Directory = v;
                    });

                    DynamicHelper.If<bool>((object)model.enabled, v => site.TraceFailedRequestsLogging.Enabled = v);
                    DynamicHelper.If((object)model.maximum_number_trace_files, 1, 10000, v => site.TraceFailedRequestsLogging.MaxLogFiles = v);
            }
        }

        public static string GetLocation(string id)
        {
            return $"/{Defines.PATH}/{id}";
        }

        public static TraceFailedRequestsSection GetTraceFailedRequestsSection(Site site, string path, string configPath = null)
        {
            return (TraceFailedRequestsSection)ManagementUnit.GetConfigSection(site?.Id,
                                                                           path,
                                                                           FailureTracingGlobals.FailureTracingSectionName,
                                                                           typeof(TraceFailedRequestsSection),
                                                                           configPath);
        }

        public static TraceProviderDefinitionsSection GetTraceProviderDefinitionSection(Site site, string path, string configPath = null)
        {
            return (TraceProviderDefinitionsSection)ManagementUnit.GetConfigSection(site?.Id,
                                                                           path,
                                                                           FailureTracingGlobals.ProviderDefinitionsSectionName,
                                                                           typeof(TraceProviderDefinitionsSection),
                                                                           configPath);
        }
    }
}
