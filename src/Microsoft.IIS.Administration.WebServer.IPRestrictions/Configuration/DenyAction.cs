// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.IPRestrictions
{

    public enum DenyActionType
    {

        Abort = 0,

        Unauthorized = 401,

        Forbidden = 403,

        NotFound = 404

    }
}
