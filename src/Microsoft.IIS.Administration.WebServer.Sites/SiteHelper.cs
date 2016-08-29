// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Sites
{
    using Core.Utils;
    using Web.Administration;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using Core;
    using AppPools;
    using System.IO;
    using System.Runtime.InteropServices;
    using Newtonsoft.Json.Linq;
    using Certificates;
    using System.Security.Cryptography.X509Certificates;
    using Core.Http;
    using System.Dynamic;


    public static class SiteHelper
    {
        private const string SCOPE_KEY = "scope";
        private static readonly Fields RefFields =  new Fields("name", "id", "status");
        private const string MaxUrlSegmentsAttribute = "maxUrlSegments";

        public static Site CreateSite(dynamic model) {
            // Ensure necessary information provided
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (DynamicHelper.Value(model.name) == null) {
                throw new ApiArgumentException("name");
            }

            //
            // Check if root directory provided for site creation via application settings
            string siteCreationRoot = ConfigurationHelper.Config.SiteCreationRoot;

            string physicalPath = DynamicHelper.Value(model.physical_path);
            if (!string.IsNullOrEmpty(physicalPath) || string.IsNullOrEmpty(siteCreationRoot) || !Directory.Exists(siteCreationRoot)) {

                if (physicalPath == null || String.IsNullOrEmpty(physicalPath)) {
                    throw new ApiArgumentException("physical_path", "Physical path is required.");
                }
                if (!Directory.Exists(System.Environment.ExpandEnvironmentVariables(physicalPath))) {
                    throw new ApiArgumentException("physical_path", "Directory does not exist.");
                }
            }
            else {
                physicalPath = Path.Combine(siteCreationRoot, DynamicHelper.Value(model.name));
                
                if(!Directory.Exists(physicalPath)) {
                    Directory.CreateDirectory(physicalPath);
                }
            }
            if (model.bindings == null) {
                throw new ApiArgumentException("bindings");
            }

            ServerManager sm = ManagementUnit.ServerManager;

            // Create site using Server Manager
            Site site = sm.Sites.CreateElement();

            // Initialize the new sites physical path. This is only touched during creation
            site.Applications.Add("/", physicalPath.Replace('/', '\\'));

            // Initialize new site settings
            SetToDefaults(site, sm.SiteDefaults);

            // Set site settings to those provided
            SetSite(site, model);

            // Initialize site Id by obtaining the first available
            site.Id = FirstAvailableId();

            return site;
        }

        // REVIEW: Safe to use the id of a site alone? This number can be reused if site is deleted
        public static Site GetSite(long id)
        {
            Site site = ManagementUnit.ServerManager.Sites.Where(s => s.Id == id).FirstOrDefault();
            return site;
        }

        public static IEnumerable<Site> GetSites(ApplicationPool pool) {
            if (pool == null) {
                throw new ArgumentNullException(nameof(pool));
            }

            var sites = new List<Site>();

            var sm = ManagementUnit.ServerManager;

            foreach (var site in sm.Sites) {
                foreach (var app in site.Applications) {
                    if (app.ApplicationPoolName.Equals(pool.Name, StringComparison.OrdinalIgnoreCase)) {
                        sites.Add(site);
                        break;
                    }
                }
            }

            return sites;
        }


        public static Site UpdateSite(long id, dynamic model)
        {
            if(model == null) {
                throw new ApiArgumentException("model");
            }

            // Obtain target site via its id number
            Site site = GetSite(id);

            // Update state of site to those specified in the model
            if(site != null) {
                SetSite(site, model);
            }

            return site;

        }

        public static void DeleteSite(Site site)
        {
            ManagementUnit.ServerManager.Sites.Remove(site);
        }

        public static object ToJsonModel(Site site, Fields fields = null)
        {
            if (site == null) {
                return null;
            }

            bool full = fields == null || !fields.HasFields;

            if(fields == null) {
                fields = Fields.All;
            }

            dynamic obj = new ExpandoObject();

            //
            // name
            if(fields.Exists("name")) {
                obj.name = site.Name;
            }

            //
            // id
            obj.id = new SiteId(site.Id).Uuid;

            //
            // physical_path
            if (fields.Exists("physical_path")) {
                string physicalPath = string.Empty;
                Application rootApp = site.Applications["/"];

                if (rootApp != null && rootApp.VirtualDirectories["/"] != null) {
                    physicalPath = rootApp.VirtualDirectories["/"].PhysicalPath;
                }

                obj.physical_path = physicalPath;
            }

            //
            // status
            if (fields.Exists("status")) {
                // Prepare state
                Status state = Status.Unknown;
                try {
                    state = StatusExtensions.FromObjectState(site.State);
                }
                catch (COMException) {
                    // Problem getting state of site. Possible reasons:
                    // 1. Site's application pool was deleted.
                    // 2. Site was just created and the status is not accessible yet.
                }
                obj.status = Enum.GetName(typeof(Status), state).ToLower();
            }

            //
            // server_auto_start
            if (fields.Exists("server_auto_start")) {
                obj.server_auto_start = site.ServerAutoStart;
            }

            //
            // enabled_protocols
            if (fields.Exists("enabled_protocols")) {
                Application rootApp = site.Applications["/"];
                obj.enabled_protocols = rootApp == null ? string.Empty : rootApp.EnabledProtocols;
            }

            //
            // limits
            if (fields.Exists("limits")) {

                dynamic limits = new ExpandoObject();

                limits.connection_timeout = site.Limits.ConnectionTimeout.TotalSeconds;
                limits.max_bandwidth = site.Limits.MaxBandwidth;
                limits.max_connections = site.Limits.MaxConnections;

                if (site.Limits.Schema.HasAttribute(MaxUrlSegmentsAttribute)) {
                    limits.max_url_segments = site.Limits.MaxUrlSegments;
                }

                obj.limits = limits;
            }

            // 
            // bindings
            if (fields.Exists("bindings")) {
                obj.bindings = site.Bindings.Select(b => ToJsonModel(b));
            }

            //
            // application_pool
            if (fields.Exists("application_pool")) {
                Application rootApp = site.Applications["/"];
                var pool = rootApp != null ? AppPoolHelper.GetAppPool(rootApp.ApplicationPoolName) : null;
                obj.application_pool = (pool == null) ? null : AppPoolHelper.ToJsonModelRef(pool);
            }

            return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj, full);
        }

        public static object ToJsonModelRef(Site site)
        {
            return ToJsonModel(site, RefFields);
        }

        public static string GetLocation(string id) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentNullException(nameof(id));
            }

            return $"/{Defines.PATH}/{id}";
        }

        public static Site ResolveSite(dynamic model = null)
        {
            Site site = null;
            string scope = null;
            string siteUuid = null;

            // Resolve from model
            if (model != null) {
                //
                // website.id                
                if (model.website != null) {
                    if (!(model.website is JObject)) {
                        throw new ApiArgumentException("website");
                    }

                    siteUuid = DynamicHelper.Value(model.website.id);
                }

                //
                // scope
                if (model.scope != null) {
                    scope = DynamicHelper.Value(model.scope);
                }
            }

            var context = HttpHelper.Current;

            //
            // Resolve {site_id} from query string
            if (siteUuid == null) {
                siteUuid = context.Request.Query[Defines.IDENTIFIER];
            }

            if (!string.IsNullOrEmpty(siteUuid)) {
                SiteId siteId = new SiteId(siteUuid);

                site = SiteHelper.GetSite(new SiteId(siteUuid).Id);
                if (site == null) {
                    throw new NotFoundException("site");
                }

                return site;
            }


            //
            // Resolve {scope} from query string
            if (scope == null) {
                scope = context.Request.Query[SCOPE_KEY];
            }

            if (!string.IsNullOrEmpty(scope)) {
                int index = scope.IndexOf('/');
                string siteName = index >= 0 ? scope.Substring(0, index) : scope;

                site = ManagementUnit.Current.ServerManager.Sites.FirstOrDefault(s => s.Name.Equals(siteName, StringComparison.OrdinalIgnoreCase));

                // Scope points to non existant site
                if(site == null) {
                    throw new ScopeNotFoundException(scope);
                }
            }

            return site;
        }


        public static string ResolvePath(dynamic model = null)
        {
            string scope = null;

            if (model != null) {
                //
                // scope
                if (model.scope != null) {
                    scope = DynamicHelper.Value(model.scope);
                }
            }

            var context = HttpHelper.Current;

            //
            // Resolve {scope} from query string
            if (scope == null) {
                scope = context.Request.Query[SCOPE_KEY];
            }

            if (scope == string.Empty) {
                return scope;
            }

            if (scope != null) {
                int index = scope.IndexOf('/');
                return index >= 0 ? scope.Substring(index) : "/";
            }

            //
            // Scope isn't specified, resolve from site root
            Site site = ResolveSite(model);

            return (site != null) ? "/" : null;
        }

        

        private static Site SetToDefaults(Site site, SiteDefaults defaults)
        {
            site.ServerAutoStart = defaults.ServerAutoStart;

            // Limits
            site.Limits.ConnectionTimeout = defaults.Limits.ConnectionTimeout;
            site.Limits.MaxBandwidth = defaults.Limits.MaxBandwidth;
            site.Limits.MaxConnections = defaults.Limits.MaxConnections;

            if (site.Limits.Schema.HasAttribute(MaxUrlSegmentsAttribute)) {
                site.Limits.MaxUrlSegments = defaults.Limits.MaxUrlSegments;
            }

            // TraceFailedRequestLogging
            site.TraceFailedRequestsLogging.Enabled = defaults.TraceFailedRequestsLogging.Enabled;
            site.TraceFailedRequestsLogging.Directory = defaults.TraceFailedRequestsLogging.Directory;
            site.TraceFailedRequestsLogging.MaxLogFiles = defaults.TraceFailedRequestsLogging.MaxLogFiles;

            return site;
        }

        private static Site SetSite(Site site, dynamic model)
        {
            Debug.Assert(site != null);
            Debug.Assert((bool)(model != null));

            DynamicHelper.If((object)model.name, v => { site.Name = v; });

            site.ServerAutoStart = DynamicHelper.To<bool>(model.server_auto_start) ?? site.ServerAutoStart;

            string physicalPath = DynamicHelper.Value(model.physical_path);

            if(physicalPath != null) {

                if (!Directory.Exists(System.Environment.ExpandEnvironmentVariables(physicalPath))) {
                    throw new ApiArgumentException("physical_path", "Directory does not exist.");
                }

                var rootApp = site.Applications["/"];
                if(rootApp == null) {
                    throw new ApiArgumentException("site/physical_path", "Root application does not exist.");
                }

                var rootVDir = rootApp.VirtualDirectories["/"];
                if(rootVDir == null) {
                    throw new ApiArgumentException("site/physical_path", "Root virtual directory does not exist.");
                }

                rootVDir.PhysicalPath = physicalPath.Replace('/', '\\');  
            }

            string enabledProtocols = DynamicHelper.Value(model.enabled_protocols);

            if(enabledProtocols != null) {
                var rootApp = site.Applications["/"];

                if(rootApp != null) {
                    rootApp.EnabledProtocols = enabledProtocols;
                }
            }

            // Limits
            if(model.limits != null) {
                dynamic limits = model.limits;

                site.Limits.MaxBandwidth = DynamicHelper.To(limits.max_bandwidth, 0, uint.MaxValue) ?? site.Limits.MaxBandwidth;
                site.Limits.MaxConnections = DynamicHelper.To(limits.max_connections, 0, uint.MaxValue) ?? site.Limits.MaxConnections;

                if (site.Limits.Schema.HasAttribute(MaxUrlSegmentsAttribute)) {
                    site.Limits.MaxUrlSegments = DynamicHelper.To(limits.max_url_segments, 0, 16383) ?? site.Limits.MaxUrlSegments;
                }

                long? connectionTimeout = DynamicHelper.To(limits.connection_timeout, 0, ushort.MaxValue);
                site.Limits.ConnectionTimeout = (connectionTimeout != null) ? TimeSpan.FromSeconds(connectionTimeout.Value) : site.Limits.ConnectionTimeout;
            }

            // Bindings
            if(model.bindings != null) {
                IEnumerable<dynamic> bindings = (IEnumerable<dynamic>)model.bindings;

                // If the user passes an object for the bindings property rather than an array we will hit an exception when we try to access any property in
                // the foreach loop.
                // This means that the bindings collection won't be deleted, so the bindings are safe from harm.

                List<Binding> newBindings = new List<Binding>();

                // Iterate over the bindings to create a new binding list
                foreach(dynamic binding in bindings) {

                    // Extract data from binding
                    string ipAddress = DynamicHelper.Value(binding.ip_address);
                    long? port = DynamicHelper.To(binding.port, 1, 65535);
                    string hostname = DynamicHelper.Value(binding.hostname) ?? "";
                    
                    bool? isHttps = DynamicHelper.To<bool>(binding.is_https);
                    string certificateHash = null;
                    string certificateStoreName = null;

                    byte[] hashBytes = null;

                    // Validate that data forms valid binding
                    if (ipAddress == null) {
                        throw new ApiArgumentException("binding.ip_address");
                    }
                    else if (port == null) {
                        throw new ApiArgumentException("binding.port");
                    }
                    else if (isHttps == null) {
                        throw new ApiArgumentException("binding.is_https");
                    }

                    if (isHttps.Value) {

                        if(binding.certificate == null || !(binding.certificate is JObject)) {
                            throw new ApiArgumentException("certificate");
                        }

                        dynamic certificate = binding.certificate;
                        string uuid = DynamicHelper.Value(certificate.id);

                        if(string.IsNullOrEmpty(uuid)) {
                            throw new ApiArgumentException("certificate.id");
                        }

                        CertificateId id = new CertificateId(uuid);
                        certificateHash = id.Thumbprint;
                        certificateStoreName = Enum.GetName(typeof(StoreName), id.StoreName);
                        
                        List<byte> bytes = new List<byte>();

                        // Decode the hex string of the certificate hash into bytes
                        for (int i = 0; i < id.Thumbprint.Length; i += 2) {
                            bytes.Add(Convert.ToByte(id.Thumbprint.Substring(i, 2), 16));
                        }

                        hashBytes = bytes.ToArray();
                    }

                    // Create binding
                    Binding newBinding = site.Bindings.CreateElement();

                    // If format of the information is incorrect throws ArgumentException but does not populate ParamName
                    newBinding.BindingInformation = $"{ipAddress}:{port}:{hostname}";
                    newBinding.Protocol = isHttps.Value ? "https" : "http";

                    // Attempting to get or set the certificate hash when the protocol is not https will raise an error -msdn
                    if (isHttps.Value) {

                        // The specified certificate must be in the store with a private key or else there will be an exception when we commit
                        newBinding.CertificateHash = hashBytes;
                        newBinding.CertificateStoreName = certificateStoreName;
                    }

                    // Add to bindings list
                    newBindings.Add(newBinding);
                }

                // All bindings have been verified and added to the list
                // Clear the old list, and add the new
                site.Bindings.Clear();
                newBindings.ForEach(binding => site.Bindings.Add(binding));
            }

            // Set the app pool
            if(model.application_pool != null) {

                // Extract the uuid from the application_pool object provided in model
                string appPoolUuid = DynamicHelper.Value(model.application_pool.id);

                // It is an error to provide an application pool object without specifying its id property
                if (appPoolUuid == null) {
                    throw new ApiArgumentException("application_pool.id");
                }

                // Create application pool id object from uuid provided, use this to obtain the application pool
                AppPools.AppPoolId appPoolId = AppPoolId.CreateFromUuid(appPoolUuid);
                ApplicationPool pool = AppPoolHelper.GetAppPool(appPoolId.Name);

                Application rootApp = site.Applications["/"];

                if(rootApp == null) {
                    throw new ApiArgumentException("application_pool", "Root application does not exist.");
                }

                // REVIEW: Should we create the root application if it doesn't exist and they specify an application pool?
                // We decided not to do this for physical_path.
                // Application pool for a site is extracted from the site's root application
                rootApp.ApplicationPoolName = pool.Name;
            }

            return site;
        }

        private static object ToJsonModel(Binding binding)
        {
            X509Certificate2 targetCert = null;
            var certs = CertificateHelper.GetCertificates(CertificateHelper.STORE_NAME, CertificateHelper.STORE_LOCATION);

            if(binding.Protocol.Equals("https")) {

                foreach(var c in certs) {
                    string thumbprint = BitConverter.ToString(binding.CertificateHash).Replace("-", string.Empty);
                    if (c.Thumbprint.Equals(thumbprint, StringComparison.OrdinalIgnoreCase)) {
                        targetCert = c;
                        break;
                    }
                }
            }

            return new {
                ip_address = binding.EndPoint.Address.Equals(IPAddress.Any) ? "*" : binding.EndPoint.Address.ToString(),
                port = binding.EndPoint.Port,
                hostname = binding.Host,
                is_https = binding.Protocol.Equals("https"),
                certificate = CertificateHelper.ToJsonModelRef(targetCert, CertificateHelper.STORE_NAME, CertificateHelper.STORE_LOCATION)
            };
        }

        private static long FirstAvailableId()
        {
            ServerManager sm = ManagementUnit.ServerManager;
            for(long id = 1; id <= long.MaxValue; id++) {
                if(!sm.Sites.Any(site => site.Id == id)) {
                    return id;
                }
            }
            // REVIEW: Exception type
            throw new Exception("No available Id");
        }
    }
}
