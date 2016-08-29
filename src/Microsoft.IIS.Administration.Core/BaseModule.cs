// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core
{
    using System.Diagnostics;
    using System.Reflection;

    public abstract class BaseModule : IModule
    {
        private string _version;

        public abstract void Start();

        public virtual void Stop() { }

        public virtual string Version {
            get {
                if(this._version == null) {
                    this._version = FileVersionInfo.GetVersionInfo(this.GetType().GetTypeInfo().Assembly.Location).ProductVersion;
                }

                return this._version;
            }

            set {
                this._version = value;
            }
        }
    }
}
