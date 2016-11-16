// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using AspNetCore.Http;
    using System.IO;
    using System.Threading.Tasks;
    using AspNetCore.StaticFiles;
    using AspNetCore.Mvc;
    using System.Net;
    using System;
    using Core;
    using System.Text.Encodings.Web;

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
            if (!fileInfo.Exists) {
                throw new FileNotFoundException(fileInfo.Name);
            }

            bool isRangeRequest;
            int start = -1, finish = -1;
            var etag = ETag.Create(fileInfo);
            const string rangePrefix = "bytes=";
            isRangeRequest = context.Request.Headers.ContainsKey("range");

            // Validate
            if (isRangeRequest) {
                string range = context.Request.Headers["range"];
                range = range.Trim(' ');

                if (range.IndexOf(rangePrefix, StringComparison.OrdinalIgnoreCase) != 0) {
                    throw new RangeInvalidApiException();
                }
                range = range.Remove(0, rangePrefix.Length);
                var parts = range.Split('-');
                if (parts.Length != 2 || !int.TryParse(parts[0], out start) || !int.TryParse(parts[1], out finish)) {
                    throw new RangeInvalidApiException();
                }
                if (start < 0 || finish >= fileInfo.Length || start > finish) {
                    throw new RangeInvalidApiException();
                }
            }

            //
            // Content Type
            context.Response.ContentType = FileExtensionContentTypeProvider.Mappings.ContainsKey(fileInfo.Extension) ?
                                                FileExtensionContentTypeProvider.Mappings[fileInfo.Extension] : "text/plain";

            //
            // Content-Disposition (file name)
            context.Response.Headers.Add("Content-Disposition", $"inline;filename={UrlEncoder.Default.Encode(fileInfo.Name)}");
            
            //
            // Accept Ranges
            context.Response.Headers.Add("Accept-Ranges", "bytes");

            //
            // ETag
            context.Response.Headers.Add("ETag", etag.Value);

            // If the entity tag does not match, then the server SHOULD return the entire entity using a 200 (OK) response.
            if (isRangeRequest && ValidateIfRange(context.Request.Headers, etag)) {
                await RangeContentResponse(context, fileInfo, start, finish);
            }
            else {
                await FullContentResponse(context, fileInfo);
            }

            return new EmptyResult();
        }

        public static async Task<IActionResult> PutFileContentAsync(this HttpContext context, FileInfo fileInfo)
        {
            int start = -1, finish = -1, outOf = -1;            
            bool isRangeRequest = context.Request.Headers.ContainsKey("content-range");
            const string rangePrefix = "bytes ";

            // Content-Range: bytes {start}-{finish}/{outOf}

            // Validate
            if (isRangeRequest) {
                string sstart = null, sfinish = null, soutOf = null, range = null;
                range = context.Request.Headers["content-range"].ToString().Trim(' ');

                if (range.IndexOf(rangePrefix, StringComparison.OrdinalIgnoreCase) == 0) {
                    int index;
                    range = range.Remove(0, rangePrefix.Length);

                    index = range.IndexOf('-');
                    if (index != -1) {
                        sstart = range.Substring(0, index);
                        range = range.Remove(0, index + 1);
                    }
                    index = range.IndexOf('/');
                    if (index != -1) {
                        sfinish = range.Substring(0, index);
                        range = range.Remove(0, index + 1);
                    }
                    soutOf = range;

                    int.TryParse(sstart, out start);
                    int.TryParse(sfinish, out finish);
                    int.TryParse(sfinish, out outOf);

                    if (!int.TryParse(sstart, out start) ||
                            !int.TryParse(sfinish, out finish) ||
                            !int.TryParse(soutOf, out outOf) ||
                            start < 0 ||
                            start > fileInfo.Length ||
                            start > finish ||
                            finish >= outOf) {
                        throw new ApiArgumentException("Content-Length");
                    }
                }
            }

            if (isRangeRequest) {
                using (Stream stream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Write)) {
                    var length = finish - start + 1;
                    stream.Seek(start, SeekOrigin.Begin);
                    await context.Request.Body.CopyToAsync(stream);
                }
            }
            else {
                using (Stream stream = new FileStream(fileInfo.FullName, FileMode.Truncate, FileAccess.Write)) {
                    await context.Request.Body.CopyToAsync(stream);
                }
            }


            context.Response.StatusCode = (int)HttpStatusCode.OK;
            return new EmptyResult();
        }



        private static bool ValidateIfRange(IHeaderDictionary headers, ETag etag)
        {
            bool isIfRange = headers.ContainsKey("if-range");
            return !isIfRange || isIfRange && headers["if-range"].Equals(etag.Value);
        }

        private static async Task FullContentResponse(HttpContext context, FileInfo fileInfo)
        {
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            using (Stream stream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                await stream.CopyToAsync(context.Response.Body);
            }
        }

        private static async Task RangeContentResponse(HttpContext context, FileInfo fileInfo, int start, int finish)
        {
            //
            // Range response 206 (Partial Content)

            context.Response.StatusCode = (int)HttpStatusCode.PartialContent;
            context.Response.Headers.Add("Content-Range", $"{start}-{finish}/{fileInfo.Length}");

            using (Stream stream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                await stream.CopyRangeAsync(context.Response.Body, start, finish - start + 1);
            }
        }

        private static async Task CopyRangeAsync(this Stream src, Stream dest, int start, int length)
        {
            if (length <= 0) {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            const int bufferSize = 32 * 64;
            src.Seek(start, SeekOrigin.Begin);
            byte[] buffer = new byte[bufferSize];
            int position = start, finish = start + length - 1, read, copyAmount;

            do {
                copyAmount = finish - position + 1 < bufferSize ? finish - position + 1 : bufferSize;
                read = await src.ReadAsync(buffer, 0, copyAmount);

                if (read == 0) {
                    throw new IndexOutOfRangeException();
                }

                await dest.WriteAsync(buffer, 0, copyAmount);
                position += bufferSize;
            }
            while (position <= finish);
        }
    }

    public class RangeInvalidApiException : Exception, IError
    {
        public dynamic GetApiError()
        {
            return new
            {
                title = "Invalid Range",
                status = (int)HttpStatusCode.RequestedRangeNotSatisfiable
            };
        }
    }
}
