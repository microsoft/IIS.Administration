// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core
{
    using AspNetCore.Routing;
    using AspNetCore.Builder;
    /// <summary>
    /// The IAdminHost is the interface that is exposed to modules loaded into the IIS Management Service. Any Action that a module needs to 
    /// delegate to the top level management service application is done through the methods defined in this interface. Module's should be able to
    /// store a reference to the Management's Service implementation of the IAdminHost.
    /// </summary>
    public interface IAdminHost {
        IApplicationBuilder ApplicationBuilder { get; }
        IModule GetModuleByAssemblyName(string assemblyName);
        IRouteBuilder RouteBuilder { get; }
    }
}
