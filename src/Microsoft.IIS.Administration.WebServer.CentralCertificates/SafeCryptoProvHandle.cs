// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.CentralCertificates
{
    using System;
    using System.Runtime.InteropServices;

    sealed class SafeCryptProvHandle : SafeHandle
    {
        private static readonly IntPtr invalid = new IntPtr(-1);

        private SafeCryptProvHandle() : base(IntPtr.Zero, true) { }

        public SafeCryptProvHandle(IntPtr handle) : base(IntPtr.Zero, true)
        {
            SetHandle(handle);
        }

        public override bool IsInvalid {
            get {
                return handle == IntPtr.Zero || handle == invalid;
            }
        }

        protected override bool ReleaseHandle()
        {
            return Interop.CryptReleaseContext(handle, 0);
        }
    }
}
