// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    using AspNetCore.Mvc.Filters;
    using System;

    public class RequireGlobalModuleAttribute : ActionFilterAttribute
    {
        private string _moduleName;
        private string _displayName;

        public RequireGlobalModuleAttribute(string moduleName, string displayName = null)
        {
            if (string.IsNullOrWhiteSpace(moduleName)) {
                throw new ArgumentNullException(moduleName);
            }

            _moduleName = moduleName;
            _displayName = displayName;
        }

        public override void OnActionExecuting(ActionExecutingContext actionContext)
        {
            if (!FeaturesUtility.GlobalModuleExists(_moduleName)) {
                throw new FeatureNotFoundException(_displayName ?? _moduleName);
            }
        }
    }
}                
