// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using AspNetCore.Http;
    using Core;
    using Core.Http;
    using Core.Utils;
    using Serilog;
    using System;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Security.Cryptography;
    using System.Threading.Tasks;

    public class HttpFileHandler
    {
        private const string RangePrefix = "bytes=";
        private const string ContentRangePrefix = "bytes ";
        private FileInfo _file;
        private IFileProvider _service;
        private HttpContext _context;
        private IHeaderDictionary _customHeaders;

        public HttpFileHandler(IFileProvider fileProvider, HttpContext context, string filePath, IHeaderDictionary customHeaders = null)
        {
            if (fileProvider == null) {
                throw new ArgumentNullException(nameof(fileProvider));
            }
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (string.IsNullOrEmpty(filePath)) {
                throw new ArgumentNullException(nameof(filePath));
            }

            _context = context;
            _service = fileProvider;
            _customHeaders = customHeaders;

            _file = _service.GetFileInfo(filePath);
        }

        public async Task WriteFileContent()
        {
            int start = -1, finish = -1;
            var etag = ETag.Create(_file);
            bool isRangeRequest = _context.Request.Headers.ContainsKey(HeaderNames.Range);

            // Validate
            if (isRangeRequest) {
                ValidateRange(out start, out finish);
            }

            AddFileContentHeaders(etag);

            if (IsCachedIfModifiedSince() || IsCachedIfNoneMatch(etag)) {
                _context.Response.StatusCode = (int)HttpStatusCode.NotModified;
            }

            // If the entity tag does not match, then the server SHOULD return the entire entity using a 200 (OK) response.
            if (isRangeRequest && IsValidIfRange(etag)) {
                await RangeContentResponse(_context, _file, start, finish);
            }
            else {
                await FullContentResponse(_context, _file.FullName);
            }
        }

        public async Task PutFileContent()
        {
            _service.EnsureAccess(_file.FullName, FileAccess.Write);

            int start, finish, outOf;
            var etag = ETag.Create(_file);

            ValidateIfMatch();
            ValidateIfNoneMatch(etag);
            ValidateIfUnmodifiedSince();
            ValidateContentRange(out start, out finish, out outOf);

            string path = await CreateTempCopy(_file.FullName);

            try {
                using (var stream = _service.GetFile(path, FileMode.Open, FileAccess.Write, FileShare.Read)) {

                    if (start >= 0) {
                        //
                        // Range request

                        int length = finish - start + 1;
                        stream.Seek(start, SeekOrigin.Begin);
                    }
                    
                    await _context.Request.Body.CopyToAsync(stream);

                    //
                    // https://github.com/dotnet/corefx/blob/ec2a6190efa743ab600317f44d757433e44e859b/src/System.IO.FileSystem/src/System/IO/FileStream.Win32.cs#L1687
                    // Unlike Flush(), FlushAsync() always flushes to disk. This is intentional.
                    stream.Flush();
                }

                SwapFiles(path, _file.FullName);
            }
            catch (IndexOutOfRangeException) {
                throw new ApiArgumentException(HeaderNames.ContentLength);
            }
            finally {
                _service.DeleteFile(path);
            }

            _context.Response.StatusCode = (int) HttpStatusCode.OK;
        }

        public void WriteFileContentHeaders()
        {
            int start = -1, finish = -1;
            var etag = ETag.Create(_file);
            bool isRangeRequest = _context.Request.Headers.ContainsKey(HeaderNames.Range);

            // Validate
            if (isRangeRequest) {
                ValidateRange(out start, out finish);
            }

            AddFileContentHeaders(etag);

            //
            // Content Length
            if (isRangeRequest) {
                _context.Response.ContentLength = finish - start + 1;
            }
            else {
                _context.Response.ContentLength = _file.Length;
            }

            if (IsCachedIfModifiedSince() || IsCachedIfNoneMatch(etag)) {
                _context.Response.StatusCode = (int)HttpStatusCode.NotModified;
            }
        }



        private void AddFileContentHeaders(ETag etag)
        {
            //
            // Content Type
            _context.Response.ContentType = "application/octet-stream";

            //
            // Content-Disposition (file name)
            _context.Response.Headers.SetContentDisposition(false, _file.Name);

            //
            // Accept Ranges
            _context.Response.Headers.Add(HeaderNames.AcceptRanges, "bytes");

            //
            // Last Modified
            _context.Response.Headers.Add(HeaderNames.LastModified, _file.LastWriteTimeUtc.ToString("r"));

            //
            // ETag
            _context.Response.Headers.Add(HeaderNames.ETag, etag.Value);

            if (IsCachedIfModifiedSince()) {

                //
                // Date
                if (!_context.Response.Headers.ContainsKey(HeaderNames.Date)) {
                    _context.Response.Headers.Add(HeaderNames.Date, DateTime.UtcNow.ToString());
                }
            }

            SetCustomHeaders();
        }

        private void SetCustomHeaders()
        {
            if (_customHeaders != null) {
                foreach (var header in _customHeaders) {
                    _context.Response.Headers[header.Key] = header.Value;
                }
            }
        }

        private bool IsCachedIfModifiedSince()
        {
            bool result = false;
            DateTime ifModifiedSince = default(DateTime);
            IHeaderDictionary reqHeaders = _context.Request.Headers;

            // Trim milliseconds
            var lastModified = _file.LastWriteTimeUtc.AddTicks( - (_file.LastWriteTimeUtc.Ticks % TimeSpan.TicksPerSecond));
            

            if (reqHeaders.ContainsKey(HeaderNames.IfModifiedSince)
                    && DateTime.TryParse(reqHeaders[HeaderNames.IfModifiedSince],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal,
                    out ifModifiedSince)
                    && ifModifiedSince.ToUniversalTime() >= lastModified) {
                        result = true;
                    }

            return result;
        }

        private bool IsCachedIfNoneMatch(ETag etag)
        {
            var headers = _context.Request.Headers;

            var isIfNoneMatch = headers.ContainsKey(HeaderNames.IfNoneMatch);
            return isIfNoneMatch && (headers[HeaderNames.IfNoneMatch].Equals(etag.Value) || headers[HeaderNames.IfNoneMatch].Equals("*"));
        }

        private bool IsValidIfRange(ETag etag)
        {
            var headers = _context.Request.Headers;

            bool isIfRange = headers.ContainsKey(HeaderNames.IfRange);
            return !isIfRange || headers[HeaderNames.IfRange].Equals(etag.Value);
        }

        private void ValidateIfMatch()
        {
            var headers = _context.Request.Headers;

            if (headers.ContainsKey(HeaderNames.IfMatch)) {
                var ifMatch = headers[HeaderNames.IfMatch].ToString().Trim();

                if (!ifMatch.Equals(ETag.Create(_file).Value)) {
                    throw new PreconditionFailedException(HeaderNames.IfMatch);
                }
            }
        }

        private void ValidateIfUnmodifiedSince()
        {
            var headers = _context.Request.Headers;
            DateTime unmodifiedSince;

            if (headers.ContainsKey(HeaderNames.IfUnmodifiedSince) && DateTime.TryParse(headers[HeaderNames.IfUnmodifiedSince], out unmodifiedSince)) {
                unmodifiedSince = unmodifiedSince.ToUniversalTime();

                if (_file.LastWriteTimeUtc > unmodifiedSince) {
                    throw new PreconditionFailedException(HeaderNames.IfUnmodifiedSince);
                }
            }
        }

        private void ValidateIfNoneMatch(ETag etag)
        {
            if (IsCachedIfNoneMatch(etag)) {
                throw new PreconditionFailedException(HeaderNames.IfNoneMatch);
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

            start = finish = outOf = -1;

            if (!_context.Request.Headers.ContainsKey(HeaderNames.ContentRange)) {
                return;
            }

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
                try {
                    await CopyRangeAsync(stream, context.Response.Body, start, finish - start + 1);
                }
                catch(IndexOutOfRangeException) {
                    throw new ApiArgumentException(HeaderNames.Range);
                }
            }
        }

        private static async Task CopyRangeAsync(Stream src, Stream dest, int start, int length)
        {
            if (length <= 0) {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            const int bufferSize = 4096;
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

        private async Task<string> CreateTempCopy(string path)
        {
            string tempFilePath = GetTempFilePath(path);
            await _service.CopyFile(path, tempFilePath, true);

            return tempFilePath;
        }

        private void SwapFiles(string pathA, string pathB)
        {
            var fileATempPath = GetTempFilePath(pathA);
            _service.MoveFile(pathA, fileATempPath);
            _service.MoveFile(pathB, pathA);
            _service.MoveFile(fileATempPath, pathB);
        }

        private string GetTempFilePath(string path)
        {
            string tempFileName = null;
            var parts = path.Split(PathUtil.SEPARATORS);
            var fileName = parts[parts.Length - 1];

            parts[parts.Length - 1] = string.Empty;
            var parentPath = string.Join(Path.DirectorySeparatorChar.ToString(), parts);

            do {
                tempFileName = GetTempName(fileName);
            }
            while (_service.FileExists(Path.Combine(parentPath, tempFileName)));

            parts[parts.Length - 1] = tempFileName;
            return string.Join(Path.DirectorySeparatorChar.ToString(), parts);
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
