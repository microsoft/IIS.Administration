// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Delegation
{
    using Core;
    using System;

    public class Defines
    {
        private const string ENDPOINT = "feature-delegation";

        public const string DelegationsName = "Microsoft.WebServer.DelegationSections";
        public const string DelegationName = "Microsoft.WebServer.DelegationSection";
        public static readonly string PATH = $"{WebServer.Defines.PATH}/{ENDPOINT}";
        public static readonly ResDef Resource = new ResDef("delegation", new Guid("51B2049B-915E-47C6-B73C-E864954BAD65"), ENDPOINT);
        public const string IDENTIFIER = "section.id";
    }
}
