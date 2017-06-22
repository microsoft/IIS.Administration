// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer {
    using AspNetCore.Mvc.Filters;
    using Microsoft.IIS.Administration.Core;


    public class RequireWebServerAttribute : ActionFilterAttribute {
        private const string NAME = "Web Server (IIS)";
        private bool _require;

        public RequireWebServerAttribute(bool require = true) {
            _require = require;
        }

        public override void OnActionExecuting(ActionExecutingContext actionContext) {

            if (_require) {
                if (!FeaturesUtility.HasAnyGlobalModule()) {
                    throw new FeatureNotFoundException(NAME);
                }
            }
            else {
                if (FeaturesUtility.HasAnyGlobalModule()) {
                    throw new AlreadyExistsException(NAME);
                }
            }
        }
    }
}                
