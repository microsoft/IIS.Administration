// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using AspNetCore.Http;
    using AspNetCore.Mvc;
    using System.IO;
    using System.Threading.Tasks;

    public static class HttpContextFileExtensions
    {
        public static async Task<IActionResult> GetFileContentAsync(this HttpContext context, FileInfo fileInfo)
        {
            var handler = new HttpFileHandler(context, fileInfo);

            return await handler.GetFileContentAsync();
        }

        public static async Task<IActionResult> PutFileContentAsync(this HttpContext context, FileInfo fileInfo)
        {
            var handler = new HttpFileHandler(context, fileInfo);

            return await handler.PutFileContentAsync();
        }
    }
}
