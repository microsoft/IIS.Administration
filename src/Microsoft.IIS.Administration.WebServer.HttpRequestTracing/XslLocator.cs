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

    sealed class XslLocator
    {
        private const string _path = "static/Microsoft.IIS.Administration.WebServer.HttpRequestTracing/freb.xsl";
        private string _physicalPath;
        private IApplicationHostConfigProvider _configProvider;

        public XslLocator(IHostingEnvironment env, IApplicationHostConfigProvider configProvider)
        {
            _physicalPath = Path.Combine(env.WebRootPath, _path);
            _configProvider = configProvider;
        }

        public string GetPath()
        {
            if (!File.Exists(_physicalPath)) {
                CopyXsl();
            }

            return "/" + _path;
        }

        private void CopyXsl()
        {
            using (var sm = new ServerManager(true, _configProvider?.Path)) {
                foreach (var site in sm.Sites) {
                    try {
                        string dir = Environment.ExpandEnvironmentVariables(site.TraceFailedRequestsLogging.Directory);
                        if (Directory.Exists(dir)) {
                            string file = Directory.GetFiles(dir, "freb.xsl", SearchOption.AllDirectories).FirstOrDefault();
                            if (string.IsNullOrEmpty(file)) {
                                continue;
                            }
                            Directory.CreateDirectory(Path.GetDirectoryName(_physicalPath));
                            File.Copy(file, _physicalPath, true);
                            break;
                        }
                    }
                    catch (Exception e) {
                        Log.Error(e, "Copy freb.xsl failed.");
                        continue;
                    }
                }
            }
        }
    }
}