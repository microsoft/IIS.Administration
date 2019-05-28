// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration {
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Threading.Tasks;
    using AspNetCore.Http;
    using Core;
    using Core.Http;
    using Core.Utils;
    using Newtonsoft.Json;
    using Serilog;
    using Serilog.Events;

    public class ErrorHandler {
        RequestDelegate _next;

        public ErrorHandler(RequestDelegate next) {
            _next = next;
        }

        public async Task Invoke(HttpContext context) {

            try {
                await _next(context);
            }
            catch (Exception e) {
                //
                // Try to handle known Api Errors
                IError error = e as IError;
                if (error != null)
                {
                    await HandleApiError(error, context);
                    return;
                }
                else
                {
                    using (var log = new EventLog("Application"))
                    {
                        log.Source = Program.EventSourceName;
                        log.WriteEntry($"Microsoft IIS Administration API encountered an unexpected error: {e.ToString()}", EventLogEntryType.Error);
                    }
                }
                throw;
            }
        }

        private static async Task HandleApiError(IError error, HttpContext context) {
            object apiError = error.GetApiError();

            if (apiError == null) {
                return;
            }

            var response = context.Response;

            int status = (int)((dynamic)apiError.ToExpando()).status;

            // Set Status
            response.StatusCode = status;

            //
            // Set headers
            response.ContentType = JsonProblem.CONTENT_TYPE;
            response.Headers[Net.Http.Headers.HeaderNames.ContentLanguage] = JsonProblem.CONTENT_LANG;

            if (status == (int)HttpStatusCode.UnsupportedMediaType) {
                response.Headers[HeaderNames.AcceptPatch] = JsonProblem.JSON_CONTENT_TYPE;
            }

            //
            // Write content
            var e = JsonConvert.SerializeObject(apiError);
            LogError(e);
            await response.WriteAsync(e);
        }

        private static void LogError(string errorContent)
        {
            if (Log.Logger.IsEnabled(LogEventLevel.Information)) {
                Task.Run(() => {
                    Log.Logger.Information($"{errorContent}{System.Environment.NewLine}{System.Environment.NewLine}");
                });
            }
        }
    }
}
