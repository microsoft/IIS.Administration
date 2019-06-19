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
    using Core.Http;
    using System.Dynamic;
    using Files;
    using CentralCertificates;

    public static class SiteHelper
    {
        private const string OIDServerAuth = "1.3.6.1.5.5.7.3.1";
        private const string SCOPE_KEY = "scope";
        private static readonly Fields RefFields =  new Fields("name", "id", "status");
        private const string sslFlagsAttribute = "sslFlags";
        private const string MaxUrlSegmentsAttribute = "maxUrlSegments";

        public static Site CreateSite(dynamic model, IFileProvider fileProvider) {
            // Ensure necessary information provided
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (DynamicHelper.Value(model.name) == null) {
                throw new ApiArgumentException("name");
            }
            if (string.IsNullOrEmpty(DynamicHelper.Value(model.physical_path))) {
                throw new ApiArgumentException("physical_path");
            }
            if (model.bindings == null) {
                throw new ApiArgumentException("bindings");
            }

            ServerManager sm = ManagementUnit.ServerManager;

            // Create site using Server Manager
            Site site = sm.Sites.CreateElement();

            // Initialize the new sites physical path. This is only touched during creation
            site.Applications.Add("/", string.Empty);

            // Initialize new site settings
            SetToDefaults(site, sm.SiteDefaults);

            // Initialize site Id by obtaining the first available
            site.Id = FirstAvailableId();

            // Set site settings to those provided
            SetSite(site, model, fileProvider);

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


        public static Site UpdateSite(long id, dynamic model, IFileProvider fileProvider)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            // Obtain target site via its id number
            Site site = GetSite(id);

            // Update state of site to those specified in the model
            if (site != null) {
                SetSite(site, model, fileProvider);
            }

            return site;

        }

        public static void DeleteSite(Site site)
        {
            ManagementUnit.ServerManager.Sites.Remove(site);
        }

        internal static object ToJsonModel(Site site, Fields fields = null, bool full = true)
        {
            if (site == null) {
                return null;
            }

            if (fields == null) {
                fields = Fields.All;
            }

            dynamic obj = new ExpandoObject();
            var siteId = new SiteId(site.Id);

            //
            // name
            if (fields.Exists("name")) {
                obj.name = site.Name;
            }

            //
            // id
            obj.id = siteId.Uuid;

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
            // key
            if (fields.Exists("key")) {
                obj.key = siteId.Id;
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
                var bindings = new List<object>();

                foreach (Binding b in site.Bindings) {
                    bindings.Add(ToJsonModel(b));
                }

                obj.bindings = bindings;
            }

            //
            // application_pool
            if (fields.Exists("application_pool")) {
                Application rootApp = site.Applications["/"];
                var pool = rootApp != null ? AppPoolHelper.GetAppPool(rootApp.ApplicationPoolName) : null;
                obj.application_pool = (pool == null) ? null : AppPoolHelper.ToJsonModelRef(pool, fields.Filter("application_pool"));
            }

            return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj, full);
        }

        public static object ToJsonModelRef(Site site, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return ToJsonModel(site, RefFields, false);
            }
            else {
                return ToJsonModel(site, fields, false);
            }
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
                if (site == null) {
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

        private static Site SetSite(Site site, dynamic model, IFileProvider fileProvider)
        {
            Debug.Assert(site != null);
            Debug.Assert((bool)(model != null));

            //
            // Name
            DynamicHelper.If((object)model.name, v => { site.Name = v; });

            //
            // Server Auto Start
            site.ServerAutoStart = DynamicHelper.To<bool>(model.server_auto_start) ?? site.ServerAutoStart;

            //
            // Key
            long? key = DynamicHelper.To<long>(model.key);
            if (key.HasValue) {
                if (ManagementUnit.ServerManager.Sites.Any(s => s.Id == key.Value && site.Id != key.Value)) {
                    throw new AlreadyExistsException("key");
                }
                site.Id = key.Value;
            }

            //
            // Physical Path
            string physicalPath = DynamicHelper.Value(model.physical_path);

            if (physicalPath != null) {

                physicalPath = physicalPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                var expanded = System.Environment.ExpandEnvironmentVariables(physicalPath);

                if (!PathUtil.IsFullPath(expanded)) {
                    throw new ApiArgumentException("physical_path");
                }
                if (!fileProvider.IsAccessAllowed(expanded, FileAccess.Read)) {
                    throw new ForbiddenArgumentException("physical_path", physicalPath);
                }
                if (!Directory.Exists(expanded)) {
                    throw new NotFoundException("physical_path");
                }

                var rootApp = site.Applications["/"];
                if (rootApp != null) {

                    var rootVDir = rootApp.VirtualDirectories["/"];

                    if (rootVDir != null) {

                        rootVDir.PhysicalPath = physicalPath;
                    }
                }
            }

            //
            // Enabled Protocols
            string enabledProtocols = DynamicHelper.Value(model.enabled_protocols);

            if (enabledProtocols != null) {
                var rootApp = site.Applications["/"];

                if (rootApp != null) {
                    rootApp.EnabledProtocols = enabledProtocols;
                }
            }

            //
            // Limits
            if (model.limits != null) {
                dynamic limits = model.limits;

                site.Limits.MaxBandwidth = DynamicHelper.To(limits.max_bandwidth, 0, uint.MaxValue) ?? site.Limits.MaxBandwidth;
                site.Limits.MaxConnections = DynamicHelper.To(limits.max_connections, 0, uint.MaxValue) ?? site.Limits.MaxConnections;

                if (site.Limits.Schema.HasAttribute(MaxUrlSegmentsAttribute)) {
                    site.Limits.MaxUrlSegments = DynamicHelper.To(limits.max_url_segments, 0, 16383) ?? site.Limits.MaxUrlSegments;
                }

                long? connectionTimeout = DynamicHelper.To(limits.connection_timeout, 0, ushort.MaxValue);
                site.Limits.ConnectionTimeout = (connectionTimeout != null) ? TimeSpan.FromSeconds(connectionTimeout.Value) : site.Limits.ConnectionTimeout;
            }

            //
            // Bindings
            if (model.bindings != null) {
                IEnumerable<dynamic> bindings = (IEnumerable<dynamic>)model.bindings;

                // If the user passes an object for the bindings property rather than an array we will hit an exception when we try to access any property in
                // the foreach loop.
                // This means that the bindings collection won't be deleted, so the bindings are safe from harm.

                List<Binding> newBindings = new List<Binding>();

                // Iterate over the bindings to create a new binding list
                foreach (dynamic b in bindings) {
                    Binding binding = site.Bindings.CreateElement();
                    SetBinding(binding, b);

                    foreach (Binding addedBinding in newBindings) {
                        if (addedBinding.Protocol.Equals(binding.Protocol, StringComparison.OrdinalIgnoreCase) &&
                            addedBinding.BindingInformation.Equals(binding.BindingInformation, StringComparison.OrdinalIgnoreCase)) {
                            throw new AlreadyExistsException("binding");
                        }
                    }

                    // Add to bindings list
                    newBindings.Add(binding);
                }

                // All bindings have been verified and added to the list
                // Clear the old list, and add the new
                site.Bindings.Clear();
                newBindings.ForEach(binding => site.Bindings.Add(binding));
            }

            //
            // App Pool
            if (model.application_pool != null) {

                // Extract the uuid from the application_pool object provided in model
                string appPoolUuid = DynamicHelper.Value(model.application_pool.id);

                // It is an error to provide an application pool object without specifying its id property
                if (appPoolUuid == null) {
                    throw new ApiArgumentException("application_pool.id");
                }

                // Create application pool id object from uuid provided, use this to obtain the application pool
                AppPoolId appPoolId = AppPoolId.CreateFromUuid(appPoolUuid);
                ApplicationPool pool = AppPoolHelper.GetAppPool(appPoolId.Name);

                Application rootApp = site.Applications["/"];

                if (rootApp == null) {
                    throw new ApiArgumentException("application_pool", "Root application does not exist.");
                }

                // REVIEW: Should we create the root application if it doesn't exist and they specify an application pool?
                // We decided not to do this for physical_path.
                // Application pool for a site is extracted from the site's root application
                rootApp.ApplicationPoolName = pool.Name;
            }

            return site;
        }

        private static void SetBinding(Binding binding, dynamic obj) {
            string protocol = DynamicHelper.Value(obj.protocol);
            string bindingInformation = DynamicHelper.Value(obj.binding_information);
            bool? requireSni = DynamicHelper.To<bool>(obj.require_sni);

            if (protocol == null) {
                throw new ApiArgumentException("binding.protocol");
            }

            binding.Protocol = protocol;

            bool isHttp = protocol.Equals("http") || protocol.Equals("https");

            if (isHttp) {
                //
                // HTTP Binding information provides port, ip address, and hostname
                UInt16 port;
                string hostname;
                IPAddress ipAddress = null;

                if (bindingInformation == null) {
                    var ip = DynamicHelper.Value(obj.ip_address);
                    if (ip == "*") {
                        ipAddress = IPAddress.Any;
                    }
                    else if (!IPAddress.TryParse(ip, out ipAddress)) {
                        throw new ApiArgumentException("binding.ip_address");
                    }

                    UInt16? p = (UInt16?)DynamicHelper.To(obj.port, 1, UInt16.MaxValue);
                    if (p == null) {
                        throw new ApiArgumentException("binding.port");
                    }
                    port = p.Value;

                    hostname = DynamicHelper.Value(obj.hostname) ?? string.Empty;
                }
                else {
                    var parts = bindingInformation.Split(':');
                    if (parts.Length != 3) {
                        throw new ApiArgumentException("binding.binding_information");
                    }

                    if (parts[0] == "*") {
                        ipAddress = IPAddress.Any;
                    }
                    else if (!IPAddress.TryParse(parts[0], out ipAddress)) {
                        throw new ApiArgumentException("binding.binding_information");
                    }
                    
                    if (!UInt16.TryParse(parts[1], out port)) {
                        throw new ApiArgumentException("binding.binding_information");
                    }

                    hostname = parts[2];
                }

                binding.Protocol = protocol;

                // HTTPS
                if (protocol.Equals("https")) {
                    if (string.IsNullOrEmpty(hostname) && requireSni.HasValue && requireSni.Value) {
                        throw new ApiArgumentException("binding.require_sni");
                    }

                    if (obj.certificate == null || !(obj.certificate is JObject)) {
                        throw new ApiArgumentException("binding.certificate");
                    }

                    dynamic certificate = obj.certificate;
                    string uuid = DynamicHelper.Value(certificate.id);

                    if (string.IsNullOrEmpty(uuid)) {
                        throw new ApiArgumentException("binding.certificate.id");
                    }

                    CertificateId id = new CertificateId(uuid);
                    ICertificateStore store = CertificateStoreProviderAccessor.Instance?.Stores
                                .FirstOrDefault(s => s.Name.Equals(id.StoreName, StringComparison.OrdinalIgnoreCase));
                    ICertificate cert = null;

                    if (store != null) {
                        cert = store.GetCertificate(id.Id).Result;
                    }

                    if (cert == null) {
                        throw new NotFoundException("binding.certificate");
                    }

                    if (!cert.PurposesOID.Contains(OIDServerAuth)) {
                        throw new ApiArgumentException("binding.certificate", "Certificate does not support server authentication");
                    }

                    //
                    // Windows builtin store
                    if (store is IWindowsCertificateStore) {

                        // The specified certificate must be in the store with a private key or else there will be an exception when we commit
                        if (cert == null) {
                            throw new NotFoundException("binding.certificate");
                        }
                        if (!cert.HasPrivateKey) {
                            throw new ApiArgumentException("binding.certificate", "Certificate must have a private key");
                        }

                        List<byte> bytes = new List<byte>();
                        // Decode the hex string of the certificate hash into bytes
                        for (int i = 0; i < id.Id.Length; i += 2) {
                            bytes.Add(Convert.ToByte(id.Id.Substring(i, 2), 16));
                        }

                        binding.CertificateStoreName = id.StoreName;
                        binding.CertificateHash = bytes.ToArray();
                    }

                    //
                    // IIS Central Certificate store
                    else if (store is ICentralCertificateStore) {
                        string name = Path.GetFileNameWithoutExtension(cert.Alias);

                        if (string.IsNullOrEmpty(hostname) || !hostname.Replace('*', '_').Equals(name)) {
                            throw new ApiArgumentException("binding.hostname", "Hostname must match certificate file name for central certificate store");
                        }

                        binding.SslFlags |= SslFlags.CentralCertStore;
                    }

                    if (requireSni.HasValue) {
                        if (!binding.Schema.HasAttribute(sslFlagsAttribute)) {
                            // throw on IIS 7.5 which does not have SNI support
                            throw new ApiArgumentException("binding.require_sni", "SNI not supported on this machine");
                        }

                        if (requireSni.Value) {
                            binding.SslFlags |= SslFlags.Sni;
                        }
                        else {
                            binding.SslFlags &= ~SslFlags.Sni;
                        }
                    }
                }

                var ipModel = ipAddress.Equals(IPAddress.Any) ? "*" : ipAddress.ToString();
                binding.BindingInformation = $"{ipModel}:{port}:{hostname}";
            }
            else {
                //
                // Custom protocol
                if (string.IsNullOrEmpty(bindingInformation)) {
                    throw new ApiArgumentException("binding.binding_information");
                }

                binding.BindingInformation = bindingInformation;
            }
        }

        private static object ToJsonModel(Binding binding)
        {
            dynamic obj = new ExpandoObject();

            obj.protocol = binding.Protocol;
            obj.binding_information = binding.BindingInformation;

            bool isHttp = binding.Protocol.Equals("http") || binding.Protocol.Equals("https");

            if (isHttp) {
                string ipAddress = null;
                int? port = null;

                if (binding.EndPoint != null && binding.EndPoint.Address != null) {

                    port = binding.EndPoint.Port;
                    if (binding.EndPoint.Address != null) {
                        ipAddress = binding.EndPoint.Address.Equals(IPAddress.Any) ? "*" : binding.EndPoint.Address.ToString();
                    }
                }

                obj.ip_address = ipAddress;
                obj.port = port;
                obj.hostname = binding.Host;

                //
                // HTTPS
                if (binding.Protocol.Equals("https")) {
                    ICertificateStore store = null;

                    // Windows store
                    if (binding.CertificateStoreName != null) {
                        string thumbprint = binding.CertificateHash == null ? null : BitConverter.ToString(binding.CertificateHash)?.Replace("-", string.Empty);
                        store = CertificateStoreProviderAccessor.Instance?.Stores
                                    .FirstOrDefault(s => s.Name.Equals(binding.CertificateStoreName, StringComparison.OrdinalIgnoreCase));

                        // Certificate
                        if (store != null) {
                            obj.certificate = CertificateHelper.ToJsonModelRef(GetCertificate(() => store.GetCertificate(thumbprint).Result));
                        }
                    }

                    // IIS Central Certificate Store
                    else if (binding.Schema.HasAttribute(sslFlagsAttribute) && binding.SslFlags.HasFlag(SslFlags.CentralCertStore) && !string.IsNullOrEmpty(binding.Host)) {
                        ICentralCertificateStore centralStore = null;

                        if (PathUtil.IsValidFileName(binding.Host)) {
                            centralStore = CertificateStoreProviderAccessor.Instance?.Stores.FirstOrDefault(s => s is ICentralCertificateStore) as ICentralCertificateStore;
                        }

                        // Certificate
                        if (centralStore != null) {
                            obj.certificate = CertificateHelper.ToJsonModelRef(GetCertificate(() => centralStore.GetCertificateByHostName(binding.Host.Replace('*', '_')).Result));
                        }
                    }

                    //
                    // Ssl Flags
                    if (binding.Schema.HasAttribute(sslFlagsAttribute)) {
                        obj.require_sni = binding.SslFlags.HasFlag(SslFlags.Sni);
                    }
                }
            }

            return obj;
        }

        private static long FirstAvailableId()
        {
            ServerManager sm = ManagementUnit.ServerManager;
            for (long id = 1; id <= long.MaxValue; id++) {
                if (!sm.Sites.Any(site => site.Id == id)) {
                    return id;
                }
            }
            throw new Exception("No available Id");
        }

        private static ICertificate GetCertificate(Func<ICertificate> retreiver)
        {
            try {
                return retreiver();
            }
            catch (AggregateException) {
                return null;
            }
        }
    }
}
