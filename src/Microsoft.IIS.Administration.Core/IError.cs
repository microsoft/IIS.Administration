// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core {

    public interface IError {

        // HTTP APIs
        // application/problem+json
        // https://tools.ietf.org/html/draft-ietf-appsawg-http-problem-02

        dynamic GetApiError();
    }
}
