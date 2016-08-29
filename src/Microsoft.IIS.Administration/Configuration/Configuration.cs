// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core.Config
{
    using Cors;
    using Extensions.Configuration;
    using Extensions.Logging;
    using Logging;
    using System;
    using System.Collections.Generic;

    class Configuration : IConfiguration
    {
        private const string DEFAULT_HOST_NAME = "IIS Administration API";

        private Extensions.Configuration.IConfiguration _configuration;
        private IEnumerable<string> _administrativeGroups;
        private ILoggingConfiguration _logging;
        private ILoggingConfiguration _auditing;
        private ICorsConfiguration _cors;

        public Configuration(Extensions.Configuration.IConfiguration configuration)
        {
            if(configuration == null) {
                throw new ArgumentNullException(nameof(configuration));
            }

            this._configuration = configuration;
        }

        public Guid HostId {
            get {
                return ConfigurationHelper.HostId;
            }
        }

        public string HostName {
            get {
                return _configuration.GetValue<string>("host_name", DEFAULT_HOST_NAME);
            }
        }

        public IEnumerable<string> Administrators {
            get {
                if (this._administrativeGroups == null) {
                    var groups =  new List<string>();
                    ConfigurationBinder.Bind(this._configuration.GetSection("administrators"), groups);
                    this._administrativeGroups = groups;
                }
                return this._administrativeGroups;
            }
        }

        public string SiteCreationRoot {
            get {
                return _configuration.GetValue<string>("site_creation_root", string.Empty);
            }
        }

        public ILoggingConfiguration Logging {
            get {
                if(this._logging == null) {
                    this._logging = new LoggingConfiguration() {
                        Enabled = this._configuration.GetValue("logging:enabled", true),
                        LogsRoot = Environment.ExpandEnvironmentVariables(this._configuration.GetValue("logging:path", string.Empty)),
                        MinLevel = this._configuration.GetValue("logging:min_level", LogLevel.Error),
                        FileName = this._configuration.GetValue("logging:file_name", "log-{Date}.txt")
                    };
                }

                return this._logging;
            }
        }

        public ILoggingConfiguration Auditing
        {
            get {
                if (this._auditing == null) {
                    this._auditing = new LoggingConfiguration() {
                        Enabled = this._configuration.GetValue("auditing:enabled", true),
                        LogsRoot = Environment.ExpandEnvironmentVariables(this._configuration.GetValue("auditing:path", string.Empty)),
                        MinLevel = LogLevel.Information,
                        FileName = this._configuration.GetValue("auditing:file_name", "audit-{Date}.txt")
                    };
                }

                return this._auditing;
            }
        }

        public ICorsConfiguration Cors  {
            get {
                if (this._cors == null) {
                    this._cors = new CorsConfiguration() {

                        // Cannot bind configuration to uninitialized list
                        Rules = new List<Rule>()
                    };

                    ConfigurationBinder.Bind(this._configuration.GetSection("cors:rules"), this._cors.Rules);
                }

                return this._cors;
            }
        }
    }
}
