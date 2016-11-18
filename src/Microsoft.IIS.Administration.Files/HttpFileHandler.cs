// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using AspNetCore.Http;
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Core.Utils;
    using System;
    using System.IO;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;

    public class HttpFileHandler
    {
        private const string RangePrefix = "bytes=";
        private const string ContentRangePrefix = "bytes ";
        private FileInfo _file;
        private FileService _service;
        private HttpContext _context;

        public HttpFileHandler(HttpContext context, FileInfo fileInfo)
        {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (fileInfo == null) {
                throw new ArgumentNullException(nameof(fileInfo));
            }

            this._context = context;
            this._file = fileInfo;
            this._service = new FileService();
        }

        public async Task<IActionResult> GetFileContentAsync()
        {
            bool isRangeRequest;
            int start = -1, finish = -1;
            var etag = ETag.Create(_file);
            isRangeRequest = _context.Request.Headers.ContainsKey(HeaderNames.Range);

            // Validate
            if (isRangeRequest) {
                ValidateRange(out start, out finish);
            }

            //
            // Content Type
            _context.Response.ContentType = "application/octet-stream";

            //
            // Content-Disposition (file name)
            _context.Response.Headers.Add("Content-Disposition", $"inline;filename={UrlEncoder.Default.Encode(_file.Name)}");
            
            //
            // Accept Ranges
            _context.Response.Headers.Add("Accept-Ranges", "bytes");

            //
            // ETag
            _context.Response.Headers.Add("ETag", etag.Value);

            // If the entity tag does not match, then the server SHOULD return the entire entity using a 200 (OK) response.
            if (isRangeRequest && ValidateIfRange(_context.Request.Headers, etag)) {
                await RangeContentResponse(_context, _file, start, finish);
            }
            else {
                await FullContentResponse(_context, _file.FullName);
            }

            return new EmptyResult();
        }

        public async Task<IActionResult> PutFileContentAsync()
        {
            int start = -1, finish = -1, outOf = -1;
            bool isRangeRequest = _context.Request.Headers.ContainsKey(HeaderNames.ContentRange);

            ValidateIfMatch();

            if (isRangeRequest) {
                ValidateContentRange(out start, out finish, out outOf);
            }

            var tempCopy = await GetTempCopy(_file.FullName);

            if (isRangeRequest) {
                try {
                    using (var stream = _service.GetFile(tempCopy, FileMode.Open, FileAccess.Write, FileShare.Read)) {
                    var length = finish - start + 1;
                    stream.Seek(start, SeekOrigin.Begin);
                        await CopyRangeAsync(_context.Request.Body, stream, 0, length);
                    }
                }
                catch (IndexOutOfRangeException) {
                    _service.DeleteFile(tempCopy);
                    throw new ApiArgumentException(HeaderNames.ContentLength);
                }
                SwapFiles(tempCopy, _file.FullName);
                _service.DeleteFile(tempCopy);
            }
            else {
                using (var stream = _service.GetFile(tempCopy, FileMode.Truncate, FileAccess.Write, FileShare.Read)) {
                    await _context.Request.Body.CopyToAsync(stream);
                }
                SwapFiles(tempCopy, _file.FullName);
                _service.DeleteFile(tempCopy);
            }

            _context.Response.StatusCode = (int)HttpStatusCode.OK;
            return new EmptyResult();
        }



        private static bool ValidateIfRange(IHeaderDictionary headers, ETag etag)
        {
            bool isIfRange = headers.ContainsKey(HeaderNames.IfRange);
            return !isIfRange || isIfRange && headers[HeaderNames.IfRange].Equals(etag.Value);
        }

        private void ValidateIfMatch()
        {
            if (_context.Request.Headers.ContainsKey(HeaderNames.IfMatch)) {
                var ifMatch = _context.Request.Headers[HeaderNames.IfMatch].ToString().Trim();

                if (!_file.Exists || !ifMatch.Equals(ETag.Create(_file).Value)) {
                    throw new PreconditionFailedException(HeaderNames.IfMatch);
                }
            }
        }

        private void ValidateRange(out int start, out int finish)
        {
            //
            // Validate
            // Range: bytes={start}-{finish}

            string range = _context.Request.Headers[HeaderNames.Range].ToString().Trim(' ');
            string sstart = null, sfinish = null;

            if (range.IndexOf(RangePrefix, StringComparison.OrdinalIgnoreCase) == 0) {
                range = range.Remove(0, RangePrefix.Length);
                var parts = range.Split('-');
                if (parts.Length == 2) {
                    sstart = parts[0];
                    sfinish = parts[1];
                }
            }

            if (!int.TryParse(sstart, out start) ||
                    !int.TryParse(sfinish, out finish) ||
                    start < 0 ||
                    finish >= _file.Length ||
                    start > finish) {
                        throw new InvalidRangeException();
                    }
        }

        private void ValidateContentRange(out int start, out int finish, out int outOf)
        {
            //
            // Validate
            // Content-Range: bytes {start}-{finish}/{outOf}

            int index;
            string sstart = null, sfinish = null, soutOf = null, range = null;
            range = this._context.Request.Headers[HeaderNames.ContentRange].ToString().Trim(' ');

            if (range.IndexOf(ContentRangePrefix, StringComparison.OrdinalIgnoreCase) == 0) {
                range = range.Remove(0, ContentRangePrefix.Length);
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
            }

            if (!int.TryParse(sstart, out start) ||
                    !int.TryParse(sfinish, out finish) ||
                    !int.TryParse(soutOf, out outOf) ||
                    start < 0 ||
                    start > _file.Length ||
                    start > finish ||
                    finish >= outOf) {
                        throw new ApiArgumentException(HeaderNames.ContentRange);
                    }

            if (_context.Request.ContentLength == null ||
                    finish - start + 1 != _context.Request.ContentLength.Value) {
                        throw new ApiArgumentException(HeaderNames.ContentLength);
                    }
        }

        private async Task FullContentResponse(HttpContext context, string path)
        {
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            using (Stream stream = _service.GetFile(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                await stream.CopyToAsync(context.Response.Body);
            }
        }

        private async Task RangeContentResponse(HttpContext context, FileInfo fileInfo, int start, int finish)
        {
            //
            // Range response 206 (Partial Content)

            context.Response.StatusCode = (int)HttpStatusCode.PartialContent;
            context.Response.Headers.Add(HeaderNames.ContentRange, $"{start}-{finish}/{fileInfo.Length}");

            using (Stream stream = _service.GetFile(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                await CopyRangeAsync(stream, context.Response.Body, start, finish - start + 1);
            }
        }

        private static async Task CopyRangeAsync(Stream src, Stream dest, int start, int length)
        {
            if (length <= 0) {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            const int bufferSize = 32 * 64;
            byte[] buffer = new byte[bufferSize];
            int position = start, finish = start + length - 1, read, copyAmount;

            if (start != 0) {
                src.Seek(start, SeekOrigin.Begin);
            }

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

        private async Task<string> GetTempCopy(string path)
        {
            var bytes = new byte[4];
            string tempFileName = null, tempFilePath = null;

            var parts = path.Split(PathUtil.SEPARATORS);
            var fileName = parts[parts.Length - 1];

            do {
                tempFileName = GetTempName(fileName);
            }
            while (_service.FileExists(tempFileName));

            parts[parts.Length - 1] = tempFileName;
            tempFilePath = string.Join(Path.DirectorySeparatorChar.ToString(), parts);
            await _service.CopyFile(path, tempFilePath, true);

            return tempFilePath;
        }

        private void SwapFiles(string pathA, string pathB)
        {
            string tempFileName = null;
            string fileAName = null;

            var parts = pathA.Split(PathUtil.SEPARATORS);
            fileAName = parts[parts.Length - 1];

            do {
                tempFileName = GetTempName(fileAName);
            }
            while (_service.FileExists(tempFileName));

            parts[parts.Length - 1] = tempFileName;

            var fileATempPath = string.Join(Path.DirectorySeparatorChar.ToString(), parts);
            _service.MoveFile(pathA, fileATempPath);
            _service.MoveFile(pathB, pathA);
            _service.MoveFile(fileATempPath, pathB);
        }

        private static string GetTempName(string name)
        {
            var bytes = new byte[4];

            using (var rng = RandomNumberGenerator.Create()) {
                rng.GetBytes(bytes);
                return Base64.Encode(bytes) + name;
            }
        }
    }
}
