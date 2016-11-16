// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer {
    using System;
    using Web.Administration;


    public interface IManagementUnit : IDisposable {
        bool Commit();
        // ServerManager.Commit() should never be called, use IManagementUnit.Commit instead.
        ServerManager ServerManager { get; }
        string ApplicationHostConfigPath { get; }
    }
}
