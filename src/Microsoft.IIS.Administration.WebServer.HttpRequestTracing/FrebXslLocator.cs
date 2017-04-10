// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRequestTracing
{
    using AspNetCore.Hosting;
    using Serilog;
    using System;
    using System.IO;
    using System.Linq;
    using Web.Administration;

    sealed class FrebXslLocator
    {
        private string _physicalPath;
        private IApplicationHostConfigProvider _configProvider;

        public FrebXslLocator(IHostingEnvironment env, IApplicationHostConfigProvider configProvider)
        {
            _configProvider = configProvider;
        }

        public string Path {
            get {
                if (_physicalPath != null) {
                    return _physicalPath;
                }

                using (var sm = new ServerManager(true, _configProvider?.Path)) {
                    foreach (var site in sm.Sites) {
                        try {
                            string dir = Environment.ExpandEnvironmentVariables(site.TraceFailedRequestsLogging.Directory);
                            if (Directory.Exists(dir)) {
                                string file = Directory.GetFiles(dir, FrebXslFileInfo.FILE_NAME, SearchOption.AllDirectories).FirstOrDefault();
                                if (string.IsNullOrEmpty(file)) {
                                    continue;
                                }

                                _physicalPath = file;
                                return _physicalPath;
                            }
                        }
                        catch (Exception e) {
                            Log.Error(e, "Obtaining freb.xsl failed.");
                            continue;
                        }
                    }
                }
                return null;
            }
        }
    }
}