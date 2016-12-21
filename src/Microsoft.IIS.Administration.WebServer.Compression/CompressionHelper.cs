// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Compression
{
    using Core;
    using Core.Utils;
    using Files;
    using Sites;
    using System.IO;
    using Web.Administration;

    public static class CompressionHelper
    {
        public static void UpdateSettings(dynamic model, Site site, string path, string configPath = null) {
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            
            var section = GetHttpCompressionSection(site, path, configPath);
            var urlSection = GetUrlCompressionSection(site, path);

            try {

                DynamicHelper.If((object) model.directory, v => {

                    v = v.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                    var expanded = System.Environment.ExpandEnvironmentVariables(v);

                    if (!PathUtil.IsFullPath(expanded)) {
                        throw new ApiArgumentException("directory");
                    }
                    if (!FileProvider.Default.IsAccessAllowed(expanded, FileAccess.Read)) {
                        throw new ForbiddenArgumentException("directory", expanded);
                    }

                    section.Directory = v;

                });

                DynamicHelper.If<bool>((object)model.do_disk_space_limitting, v => section.DoDiskSpaceLimiting = v);
                DynamicHelper.If((object)model.max_disk_space_usage, 0, uint.MaxValue, v => section.MaxDiskSpaceUsage = v);
                DynamicHelper.If((object)model.min_file_size, 0, uint.MaxValue, v => section.MinFileSizeForComp = v);
                DynamicHelper.If<bool>((object)model.do_dynamic_compression, v => urlSection.DoDynamicCompression = v);
                DynamicHelper.If<bool>((object)model.do_static_compression, v => urlSection.DoStaticCompression = v);

                if (model.metadata != null) {

                    DynamicHelper.If<OverrideMode>((object)model.metadata.override_mode, v => {
                        section.OverrideMode = v;
                        urlSection.OverrideMode = v;
                    });
                }
            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }

        internal static object ToJsonModel(Site site, string path)
        {
            var urlSection = GetUrlCompressionSection(site, path);

            // Http Compression section has 'allowDefinition="AppHostOnly"' which means it can only be edited at webserver level
            // We factor this in when displaying the metadata
            var httpSection = GetHttpCompressionSection(site, path);
            
            bool isLocal = urlSection.IsLocallyStored || httpSection.IsLocallyStored;
            bool isLocked = urlSection.IsLocked;
            OverrideMode overrideMode = urlSection.OverrideMode;
            OverrideMode overrideModeEffective = urlSection.OverrideModeEffective;


            CompressionId compressionId = new CompressionId(site?.Id, path, isLocal);

            var obj = new {
                id = compressionId.Uuid,
                scope = site == null ? string.Empty : site.Name + path,
                metadata = ConfigurationUtility.MetadataToJson(isLocal, isLocked, overrideMode, overrideModeEffective),
                directory = httpSection.Directory,
                do_disk_space_limitting = httpSection.DoDiskSpaceLimiting,
                max_disk_space_usage = httpSection.MaxDiskSpaceUsage,
                min_file_size = httpSection.MinFileSizeForComp,
                do_dynamic_compression = urlSection.DoDynamicCompression,
                do_static_compression = urlSection.DoStaticCompression,
                website = SiteHelper.ToJsonModelRef(site)
            };

            return Environment.Hal.Apply(Defines.Resource.Guid, obj);
        }

        public static UrlCompressionSection GetUrlCompressionSection(Site site, string path, string configPath = null)
        {
            return (UrlCompressionSection)ManagementUnit.GetConfigSection(site?.Id,
                                                                           path,
                                                                           CompressionGlobals.UrlCompressionSectionName,
                                                                           typeof(UrlCompressionSection),
                                                                           configPath);
        }

        public static HttpCompressionSection GetHttpCompressionSection(Site site, string path, string configPath = null)
        {
            return (HttpCompressionSection)ManagementUnit.GetConfigSection(site?.Id,
                                                                           path,
                                                                           CompressionGlobals.HttpCompressionSectionName,
                                                                           typeof(HttpCompressionSection),
                                                                           configPath);
        }

        public static bool IsSectionLocal(Site site, string path)
        {
            var urlCompression = ManagementUnit.IsSectionLocal(site?.Id,
                                                 path,
                                                 CompressionGlobals.UrlCompressionSectionName);
            var httpCompression = ManagementUnit.IsSectionLocal(site?.Id,
                                                 path,
                                                 CompressionGlobals.HttpCompressionSectionName);
            return urlCompression || httpCompression;
        }

        public static string GetLocation(string id)
        {
            return $"/{Defines.PATH}/{id}";
        }
    }
}
