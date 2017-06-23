// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration
{
    using AspNetCore.Mvc;
    using AspNetCore.Mvc.Filters;

    sealed class Fix4960ActionFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context) { }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            var objectResult = context.Result as ObjectResult;
            if (objectResult?.Value is IActionResult) {
                context.Result = (IActionResult)objectResult.Value;
            }
        }
    }
}
