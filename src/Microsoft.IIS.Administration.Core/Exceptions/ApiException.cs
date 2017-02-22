// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core {
    using System;


    public class ApiException : Exception, IError {
        public string Name { get; private set; }

        public ApiException(string message, Exception innerException) : base(message ?? string.Empty, innerException) {
        }

        public ApiException(string message, string name, Exception innerException) : base(message ?? string.Empty, innerException) {
            this.Name = name;
        }

        public virtual dynamic GetApiError() {
            return Http.ErrorHelper.Error(Message, Name);
        }
    }
}
