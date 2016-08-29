// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    using AspNetCore.Http;

    public static class HttpContextExtensions
    {
        private const string MANAGEMENT_UNIT_KEY = "Microsoft.IIS.Administration.WebServer.MAN_UNIT";

        public static void SetManagementUnit(this HttpContext context, IManagementUnit managementUnit)
        {
            context.Items[MANAGEMENT_UNIT_KEY] = managementUnit;
        }

        public static IManagementUnit GetManagementUnit(this HttpContext context)
        {
            return (IManagementUnit)context.Items[MANAGEMENT_UNIT_KEY];
        }
    }
}
