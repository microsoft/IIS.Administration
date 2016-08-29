// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Authentication
{
    using Core;
    using System;

    public class Defines
    {
        private const string AUTHENTICATION_ENDPOINT = "authentication";
        private const string ANON_AUTH_ENDPOINT = "anonymous-authentication";
        private const string WIN_AUTH_ENDPOINT = "windows-authentication";
        private const string DIGEST_AUTH_ENDPOINT = "digest-authentication";
        private const string BASIC_AUTH_ENDPOINT = "basic-authentication";

        public const string AuthenticationName = "Microsoft.WebServer.Authentication";
        public static readonly string AUTHENTICATION_PATH = $"{WebServer.Defines.PATH}/{AUTHENTICATION_ENDPOINT}";
        public static readonly ResDef AuthenticationResource = new ResDef("authentication", new Guid("E57EC26F-3552-4ED1-A172-A453D6DAE04E"), AUTHENTICATION_ENDPOINT);
        public const string AUTHENTICATION_IDENTIFIER = "auth.id";

        public const string AnonAuthenticationName = "Microsoft.WebServer.AnonymousAuthentication";
        public static readonly string ANON_AUTH_PATH = $"{AUTHENTICATION_PATH}/{ANON_AUTH_ENDPOINT}";
        public static readonly ResDef AnonAuthResource = new ResDef("anonymous", new Guid("2DD08D76-7456-41A6-9B53-977771141276"), ANON_AUTH_ENDPOINT);
        public const string ANON_AUTH_IDENTIFIER = "anon_auth.id";

        public const string BasicAuthenticationName = "Microsoft.WebServer.BasicAuthentication";
        public static readonly string BASIC_AUTH_PATH = $"{AUTHENTICATION_PATH}/{BASIC_AUTH_ENDPOINT}";
        public static readonly ResDef BasicAuthResource = new ResDef("basic", new Guid("1FBB092F-3966-45B9-9C10-E00B06842476"), BASIC_AUTH_ENDPOINT);
        public const string BASIC_AUTH_IDENTIFIER = "basic_auth.id";

        public const string DigestAuthenticationName = "Microsoft.WebServer.DigestAuthentication";
        public static readonly string DIGEST_AUTH_PATH = $"{AUTHENTICATION_PATH}/{DIGEST_AUTH_ENDPOINT}";
        public static readonly ResDef DigestAuthResource = new ResDef("digest", new Guid("FFC368F9-1F37-42F3-91E0-4B9230BA6C85"), DIGEST_AUTH_ENDPOINT);
        public const string DIGEST_AUTH_IDENTIFIER = "digest_auth.id";

        public const string WindowsAuthenticationName = "Microsoft.WebServer.WindowsAuthentication";
        public static readonly string WIN_AUTH_PATH = $"{AUTHENTICATION_PATH}/{WIN_AUTH_ENDPOINT}";
        public static readonly ResDef WinAuthResource = new ResDef("windows", new Guid("38F2E661-BB72-469F-ADEC-41A0B8E9A427"), WIN_AUTH_ENDPOINT);
        public const string WIN_AUTH_IDENTIFIER = "win_auth.id";
    }
}
