// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer {
    using System.Net;

    static class ErrorHelper
    {

        public static dynamic InvalidScopeTypeError(string scope)
        {
            return new {
                title = "Scope is not allowed",
                scope = scope,
                status = (int)HttpStatusCode.Forbidden
            };
        }

        public static dynamic ScopeNotFoundError(string scope)
        {
            return new {
                title = "Scope not found",
                scope = scope,
                status = (int)HttpStatusCode.BadRequest
            };
        }

        public static dynamic ConfigScopeNotFoundError(ConfigScopeNotFoundException ex)
        {
            return new {
                title = "Configuration scope not found",
                detail = ex.Message ?? "",
                config_path = ex.ConfigPath ?? "",
                status = (int)HttpStatusCode.InternalServerError
            };
        }

        public static dynamic FeatureNotFoundError(string name)
        {
            return new {
                title = "Not found",
                detail = "IIS feature not installed",
                name = name,
                status = (int)HttpStatusCode.NotFound
            };
        }

        public static dynamic DismError(int exitCode, string featureName)
        {
            return new {
                title = "Server Error",
                detail = "Dism Error",
                feature = featureName,
                exit_code = exitCode.ToString("X"),
                status = (int)HttpStatusCode.InternalServerError
            };
        }

        public static dynamic InstallationError(int exitCode, string productName)
        {
            return new {
                title = "Server Error",
                detail = "Installation Error",
                product = productName,
                exit_code = exitCode.ToString("X"),
                status = (int)HttpStatusCode.InternalServerError
            };
        }

        public static dynamic DownloadError(string name)
        {
            return new {
                title = "Server Error",
                detail = "Download Error",
                product = name,
                status = (int)HttpStatusCode.InternalServerError
            };
        }
    }
}
