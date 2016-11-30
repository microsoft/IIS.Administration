// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using AspNetCore.Http;
    using AspNetCore.Mvc;
    using System.Threading.Tasks;

    public static class HttpContextFileExtensions
    {
        public static async Task<IActionResult> GetFileContentAsync(this HttpContext context, string filePath, IFileProvider fileProvider, IHeaderDictionary headers = null)
        {
            var handler = new HttpFileHandler(fileProvider, context, filePath, headers);

            return await handler.GetFileContent();
        }

        public static async Task<IActionResult> PutFileContentAsync(this HttpContext context, string filePath, IFileProvider fileProvider, IHeaderDictionary headers = null)
        {
            var handler = new HttpFileHandler(fileProvider, context, filePath, headers);

            return await handler.PutFileContent();
        }

        public static IActionResult GetFileContentHeaders(this HttpContext context, string filePath, IFileProvider fileProvider, IHeaderDictionary headers = null)
        {
            var handler = new HttpFileHandler(fileProvider, context, filePath, headers);

            return handler.GetFileContentHeaders();
        }
    }
}
