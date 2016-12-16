// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using AspNetCore.Http;
    using System.Threading.Tasks;

    public static class HttpResponseFileExtensions
    {
        public static void WriteFileContentHeaders(this HttpResponse response, string filePath, IFileProvider fileProvider, IHeaderDictionary headers = null)
        {
            var handler = new HttpFileHandler(fileProvider, response.HttpContext, filePath, headers);

            handler.WriteFileContentHeaders();
        }

        public static async Task WriteFileContentAsync(this HttpResponse response, string filePath, IFileProvider fileProvider, IHeaderDictionary headers = null)
        {
            var handler = new HttpFileHandler(fileProvider, response.HttpContext, filePath, headers);

            await handler.WriteFileContent();
        }

        public static async Task PutFileContentAsync(this HttpResponse response, string filePath, IFileProvider fileProvider, IHeaderDictionary headers = null)
        {
            var handler = new HttpFileHandler(fileProvider, response.HttpContext, filePath, headers);

            await handler.PutFileContent();
        }
    }
}
