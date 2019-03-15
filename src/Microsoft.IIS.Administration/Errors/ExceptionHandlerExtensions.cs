// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration
{
    using AspNetCore.Builder;
    using System.Threading.Tasks;

    public static class ExceptionHandlerExtensions
    {
        public static IApplicationBuilder UseErrorHandler(this IApplicationBuilder builder)
        {
            // The framework error handler allows us to control the response headers even on unhandled server errors.
            var exceptionHandlerOptions = new ExceptionHandlerOptions();
            exceptionHandlerOptions.ExceptionHandler = context => {
                return Task.CompletedTask;
            };

            //
            // Framework error handling
            //
            builder.UseExceptionHandler(exceptionHandlerOptions);

            //
            // Custom error handling
            //
            builder.UseMiddleware<ErrorHandler>();

            return builder;
        }
    }
}
