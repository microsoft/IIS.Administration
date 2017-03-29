// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using AspNetCore.Http;
    using Core;
    using Core.Http;
    using System;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using AspNetCore.StaticFiles;

    public class HttpFileHandler
    {
        private const string ContentRangePrefix = "bytes ";
        private static FileExtensionContentTypeProvider _mimeMaps;
        private IFileInfo _file;
        private IFileProvider _service;
        private HttpContext _context;
        private IHeaderDictionary _customHeaders;

        public static FileExtensionContentTypeProvider MimeMaps {
            get {
                if (_mimeMaps == null) {
                    _mimeMaps = new FileExtensionContentTypeProvider();
                }
                return _mimeMaps;
            }
        }

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

            _file = _service.GetFile(filePath);
        }

        public async Task WriteFileContent()
        {
            long start = -1, finish = -1;
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
                await FullContentResponse(_context, _file);
            }
        }

        public async Task PutFileContent()
        {
            _service.EnsureAccess(_file.Path, FileAccess.Write);

            int start, finish, outOf;
            var etag = ETag.Create(_file);

            ValidateIfMatch();
            ValidateIfNoneMatch(etag);
            ValidateIfUnmodifiedSince();
            ValidateContentRange(out start, out finish, out outOf);

            IFileInfo tempFile = CreateTempFile(_file.Path);

            try {
                using (var temp = _service.GetFileStream(tempFile, FileMode.Open, FileAccess.ReadWrite, FileShare.Read)) {
                    //
                    // Write to temp file
                    await _context.Request.Body.CopyToAsync(temp);
                    temp.Flush();
                    temp.Seek(0, SeekOrigin.Begin);

                    //
                    // Copy from temp 
                    using (var real = TryOpenFile(_file, FileMode.Open, FileAccess.Write, FileShare.Read)) {
                        if (start >= 0) {
                            //
                            // Range request
                            real.Seek(start, SeekOrigin.Begin);
                        }
                        
                        await temp.CopyToAsync(real);

                        // Truncate content-range
                        if (finish > 0 && finish == outOf - 1) {
                            real.SetLength(outOf);
                        }

                        // Truncate full content
                        if (start == -1) {
                            real.SetLength(temp.Length);
                        }

                        //
                        // https://github.com/dotnet/corefx/blob/ec2a6190efa743ab600317f44d757433e44e859b/src/System.IO.FileSystem/src/System/IO/FileStream.Win32.cs#L1687
                        // Unlike Flush(), FlushAsync() always flushes to disk. This is intentional.
                        real.Flush();
                    }
                }
            }
            catch (IndexOutOfRangeException) {
                throw new ApiArgumentException(HeaderNames.ContentLength);
            }
            finally {
                _service.Delete(tempFile);
            }

            _context.Response.StatusCode = (int) HttpStatusCode.OK;
        }

        public void WriteFileContentHeaders()
        {
            long start = -1, finish = -1;
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
                _context.Response.ContentLength = _file.Size;
            }

            if (IsCachedIfModifiedSince() || IsCachedIfNoneMatch(etag)) {
                _context.Response.StatusCode = (int)HttpStatusCode.NotModified;
            }
        }



        private void AddFileContentHeaders(ETag etag)
        {
            //
            // Content Type
            string type = null;
            if (!MimeMaps.TryGetContentType(Path.GetExtension(_file.Name), out type)) {
                type = "application/octet-stream";
            }

            _context.Response.ContentType = type;

            //
            // Content-Disposition (file name)
            _context.Response.Headers.SetContentDisposition(false, _file.Name);

            //
            // Accept Ranges
            _context.Response.Headers.Add(HeaderNames.AcceptRanges, "bytes");

            //
            // Last Modified
            _context.Response.Headers.Add(HeaderNames.LastModified, _file.LastModified.ToUniversalTime().ToString("r"));

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
            var lastModified = _file.LastModified.ToUniversalTime().AddTicks( - (_file.LastModified.ToUniversalTime().Ticks % TimeSpan.TicksPerSecond));
            

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

                if (_file.LastModified.ToUniversalTime() > unmodifiedSince) {
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

        private void ValidateRange(out long start, out long finish)
        {
            if (!_context.Request.Headers.TryGetRange(out start, out finish, _file.Size)) {
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
                    start > _file.Size ||
                    start > finish ||
                    finish >= outOf) {
                        throw new ApiArgumentException(HeaderNames.ContentRange);
                    }

            if (_context.Request.ContentLength == null ||
                    finish - start + 1 != _context.Request.ContentLength.Value) {
                        throw new ApiArgumentException(HeaderNames.ContentLength);
                    }
        }

        private async Task FullContentResponse(HttpContext context, IFileInfo file)
        {
            context.Response.ContentLength = _file.Size;
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            using (Stream stream = _service.GetFileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                await stream.CopyToAsync(context.Response.Body);
            }
        }

        private async Task RangeContentResponse(HttpContext context, IFileInfo info, long start, long finish)
        {
            //
            // Range response 206 (Partial Content)

            context.Response.ContentLength = finish - start + 1;
            context.Response.StatusCode = (int)HttpStatusCode.PartialContent;
            context.Response.Headers.Add(HeaderNames.ContentRange, $"{start}-{finish}/{info.Size}");

            using (Stream stream = _service.GetFileStream(info, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                try {
                    await CopyRangeAsync(stream, context.Response.Body, start, finish - start + 1);
                }
                catch(IndexOutOfRangeException) {
                    throw new ApiArgumentException(HeaderNames.Range);
                }
            }
        }

        private static async Task CopyRangeAsync(Stream src, Stream dest, long start, long length)
        {
            if (length <= 0) {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            int copyAmount;
            const int bufferSize = 4096;
            byte[] buffer = new byte[bufferSize];
            long position = start, finish = start + length - 1, read;

            if (start != 0) {
                src.Seek(start, SeekOrigin.Begin);
            }

            do {
                copyAmount = (int) (finish - position + 1 < bufferSize ? finish - position + 1 : bufferSize);
                read = await src.ReadAsync(buffer, 0, copyAmount);

                if (read == 0) {
                    throw new IndexOutOfRangeException();
                }

                await dest.WriteAsync(buffer, 0, copyAmount);
                position += bufferSize;
            }
            while (position <= finish);
        }

        private IFileInfo CreateTempFile(string path)
        {
            string tempFilePath = PathUtil.GetTempFilePath(path);
            IFileInfo info = _service.GetFile(tempFilePath);
            info = _service.CreateFile(info);

            return info;
        }

        private void SwapFiles(string pathA, string pathB)
        {
            var fileATempPath = PathUtil.GetTempFilePath(pathA);
            IFileInfo a = _service.GetFile(pathA);
            IFileInfo b = _service.GetFile(pathB);
            IFileInfo temp = _service.GetFile(fileATempPath);

            _service.Move(a, temp);
            _service.Move(b, a);
            _service.Move(temp, b);
        }

        private Stream TryOpenFile(IFileInfo file, FileMode mode, FileAccess access, FileShare fileShare)
        {
            Stream stream = null;
            int attempts = 10;

            //
            // Retry if file locked
            while (attempts > 0 && stream == null) {
                try {
                    stream = _service.GetFileStream(file, mode, access, fileShare);
                }
                catch (LockedException) {
                    if (--attempts > 0) {
                        Thread.Sleep(10);
                        continue;
                    }
                    throw;
                }
            }

            return stream;
        }
    }
}
