// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Tests
{
    using System.Net.Http;

    public class Globals
    {
        public const string TEST_SERVER = "https://localhost";
        public const string TEST_ROOT_PATH = @"";
        public const string TEST_PORT = "";

        public static bool Success(HttpResponseMessage responseMessage)
        {
            if ((int)responseMessage.StatusCode >= 200 && (int)responseMessage.StatusCode < 300) {
                return true;
            }
            return false;
        }
    }
}
