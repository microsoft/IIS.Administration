// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Scm
{
    using Core;
    using System;

    public class Defines
    {
        private const string ENDPOINT = "service-controller";

        public const string ServiceControllerName = "Microsoft.WebServer.ServiceController";
        public static readonly string PATH = $"{WebServer.Defines.PATH}/{ENDPOINT}";
        public static readonly ResDef Resource = new ResDef("service_controller", new Guid("770951E2-C392-459D-A59F-0561404C7A7D"), ENDPOINT);
    }
}
