// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Tests {
    using System.Net.Http;

    public class Globals {
        public static bool Success(HttpResponseMessage responseMessage) {
            if ((int)responseMessage.StatusCode >= 200 && (int)responseMessage.StatusCode < 300) {
                return true;
            }
            return false;
        }
    }
}