// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.FileSystem.Core
{
    using AspNetCore.Http;
    using System.IO;
    using System.Threading.Tasks;
    using AspNetCore.StaticFiles;
    using AspNetCore.Mvc;

    public static class HttpFileHandler
    {
        private static FileExtensionContentTypeProvider _fileExtensionContentTypeProvider;

        public static FileExtensionContentTypeProvider FileExtensionContentTypeProvider {
            get {
                if (_fileExtensionContentTypeProvider == null) {
                    _fileExtensionContentTypeProvider = new FileExtensionContentTypeProvider();
                }
                return _fileExtensionContentTypeProvider;
            }
        }


        public static async Task<IActionResult> GetFileContentAsync(this HttpContext context, FileInfo fileInfo)
        {
            context.Response.ContentType = FileExtensionContentTypeProvider.Mappings.ContainsKey(fileInfo.Extension) ?
                                                FileExtensionContentTypeProvider.Mappings[fileInfo.Extension] : "text/plain";
            context.Response.Headers.Add("Content-Disposition", $"inline; filname={fileInfo.Name}");

            using (Stream stream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                await stream.CopyToAsync(context.Response.Body);
            }

            return new OkResult();
        }
    }
}
