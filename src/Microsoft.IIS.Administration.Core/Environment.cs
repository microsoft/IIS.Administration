// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core
{
    public static class Environment
    {
        public static IAdminHost Host
        {
            get;
            set;
        }

        public static IHalService Hal
        {
            get;
            set;
        }
    }
}
