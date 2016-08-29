// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer {
    using System;
    using System.IO;
    using System.Linq;
    using Core;
    using Core.Utils;
    using Core.Http;
    using Web.Administration;


    public static class ManagementUnit {
        private const string CONFIG_SCOPE_KEY = "config_scope";        
        
        public static readonly string APP_HOST_CONFIG_SCOPE = string.Empty;

        public static IManagementUnit Current {
            get {
                return HttpHelper.Current.GetManagementUnit();
            }
        }

        public static ServerManager ServerManager {
            get {
                return Current.ServerManager;
            }
        }

        public static bool IsSectionLocal(long? siteId, string path, string sectionPath) {
            try {
                var section = GetConfigSection(siteId, path, sectionPath, typeof(ConfigurationSection), null);
                return section.IsLocallyStored;
            }
            catch (LockedException) {

                // If section locked is thrown it is because the section exists
                // locally in the target web.config and that section is locked
                return true;
            }
        }

        public static ConfigurationSection GetConfigSection(long? siteId, string path, string sectionPath, Type sectionType, string configPath = null) {
            if (string.IsNullOrEmpty(sectionPath)) {
                throw new ArgumentNullException("sectionPath");
            }

            if (siteId != null && path == null) {
                throw new ArgumentNullException("path");
            }

            ConfigurationSection section = null;

            try {

                ScopedConfiguration sConfig = GetScopedConfig(siteId, path, configPath);
                section = sConfig.Location == null ?
                                         sConfig.Configuration.GetSection(sectionPath, sectionType)
                                         : sConfig.Configuration.GetSection(sectionPath, sectionType, sConfig.Location);
            }
            catch (FileLoadException e) {
                throw new LockedException(sectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
            catch (ArgumentException e) {
                if (configPath != null) {
                    throw new ApiArgumentException(CONFIG_SCOPE_KEY, e);
                }
                throw;
            }

            return section;
        }


        public static Configuration GetConfiguration(long? siteId, string path, string configScope = null) {
            return GetScopedConfig(siteId, path, configScope).Configuration;
        }

        public static string GetLocationTag(long? siteId, string path, string configScope = null) {

            // Can return null
            return GetScopedConfig(siteId, path, configScope).Location;
        }

        public static string ResolveConfigScope(dynamic model = null) {
            string scope = null;

            if (model != null) {
                scope = DynamicHelper.Value(model.config_scope);
            }

            if (scope == null) {
                scope = HttpHelper.Current.Request.Query[CONFIG_SCOPE_KEY];
            }

            return scope;
        }




        private static Configuration GetConfig(long? siteId, string path) {
            Site site = null;

            if (siteId != null) {

                site = ServerManager.Sites.Where(s => s.Id == siteId.Value).FirstOrDefault();
                if (site == null) {
                    throw new ArgumentException($"Site doesn't exist '{siteId.Value.ToString()}'", "siteId");
                }
            }

            if (!string.IsNullOrEmpty(path)) {
                if (site == null) {
                    throw new ArgumentNullException("site");
                }

                return ServerManager.GetWebConfiguration(site.Name, path);
            }

            // Get site level config
            if (site != null) {
                return ServerManager.GetWebConfiguration(site.Name);
            }

            // Get global config
            return ServerManager.GetApplicationHostConfiguration();
        }

        private static ScopedConfiguration GetScopedConfig(long? siteId, string path, string configScope = null) {
            if (siteId != null && path == null) {
                throw new ArgumentNullException("path");
            }

            // Apphost, use site/path
            if (configScope == APP_HOST_CONFIG_SCOPE) {
                Site site = null;

                if (siteId != null) {

                    site = ServerManager.Sites.Where(s => s.Id == siteId.Value).FirstOrDefault();

                    if (site == null) {
                        throw new ArgumentException($"Site doesn't exist '{siteId.Value.ToString()}'", "siteId");
                    }
                }

                var config = GetConfig(null, null);

                return site == null ? new ScopedConfiguration(config, null) : new ScopedConfiguration(config, site.Name + path);
            }
            // Config path takes precedence
            else if (configScope != null) {
                if (siteId == null) {
                    throw new ArgumentNullException("siteId");
                }

                // Find site
                Site site = ServerManager.Sites.Where(s => s.Id == siteId.Value).FirstOrDefault();
                if (site == null) {
                    throw new ArgumentException($"Site doesn't exist '{siteId.Value.ToString()}'", "siteId");
                }

                int siteNameIndex = configScope.IndexOf(site.Name, StringComparison.OrdinalIgnoreCase);
                if (siteNameIndex != 0) {
                    throw new ArgumentException("Config path does not begin with site name", "configPath");
                }

                // If configPath is same length as site name then target is config at site level
                var config = GetConfig(siteId, configScope.Length == site.Name.Length ? "/" : configScope.Substring(site.Name.Length));

                string fullPath = $"{site.Name}{path}";

                // Define location path
                int i = fullPath.IndexOf(configScope, StringComparison.OrdinalIgnoreCase);
                if (i != 0) {
                    throw new ArgumentException("Full path doesn't start with 'configPath'", "configPath");
                }

                string locationPath = fullPath.Substring(configScope.Length).TrimStart('/');

                if (locationPath.Length > 0) {
                    return new ScopedConfiguration(config, locationPath);
                }
                else {
                    return new ScopedConfiguration(config, null);
                }
            }
            // No config path
            else {
                var config = GetConfig(siteId, path);
                return new ScopedConfiguration(config, null);
            }
        }

        class ScopedConfiguration {
            public Configuration Configuration { get; private set; }
            public string Location { get; private set; }

            public ScopedConfiguration(Configuration config, string location) {
                if (config == null) {
                    throw new ArgumentNullException("config");
                }
                this.Configuration = config;
                this.Location = location;
            }
        }
    }
}
