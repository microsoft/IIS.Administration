// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core {
    using System;


    public class LockedException : Exception, IError {
        public string Name { get; private set; }

        public LockedException(string name, Exception innerException = null) : base(name, innerException) {
            Name = name;
        }

        public dynamic GetApiError() {
            return Http.ErrorHelper.LockedError(Name);
        }
    }
}
