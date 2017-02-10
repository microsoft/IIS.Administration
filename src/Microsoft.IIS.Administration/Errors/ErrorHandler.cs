// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration {
    using System;
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
                if (error != null) {
                    await HandleApiError(error, context);
                    return;
                }

                //
                // If it's not a handled/known exception log it
                LogError(e);
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
            if (Log.Logger.IsEnabled(LogEventLevel.Information))
            {
                await Task.Run(() => {
                    Log.Logger.Information($"{e}{System.Environment.NewLine}{System.Environment.NewLine}");
                });
            }
            await response.WriteAsync(e);
        }

        private void LogError(Exception e) {
            //
            // If it's not a handled/known exception log it
            if (Log.Logger.IsEnabled(LogEventLevel.Error)) {
                Task.Run(() => {
                    Log.Logger.Error($"{e.Message}{System.Environment.NewLine}\t{e.StackTrace}{System.Environment.NewLine}{System.Environment.NewLine}");
                });                
            }
        }
    }
}
