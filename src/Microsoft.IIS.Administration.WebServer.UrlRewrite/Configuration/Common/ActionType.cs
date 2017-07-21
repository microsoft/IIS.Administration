// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Microsoft.IIS.Administration.Core;
    using System;

    // Keep public for resolution of enums from 'dynamic' types in helper classes i.e. DynamicHelper
    public enum ActionType
    {
        None = 0,
        Rewrite = 1,
        Redirect = 2,
        CustomResponse = 3,
        AbortRequest = 4,
    }

    class ActionTypeHelper
    {
        public static string ToJsonModel(ActionType actionType)
        {
            switch (actionType) {
                case ActionType.None:
                    return "none";
                case ActionType.Rewrite:
                    return "rewrite";
                case ActionType.Redirect:
                    return "redirect";
                case ActionType.CustomResponse:
                    return "custom_response";
                case ActionType.AbortRequest:
                    return "abort_request";
                default:
                    throw new ArgumentException(nameof(actionType));
            }
        }

        public static ActionType FromJsonModel(string model)
        {
            switch (model.ToLowerInvariant()) {
                case "none":
                    return ActionType.None;
                case "rewrite":
                    return ActionType.Rewrite;
                case "redirect":
                    return ActionType.Redirect;
                case "custom_response":
                    return ActionType.CustomResponse;
                case "abort_request":
                    return ActionType.AbortRequest;
                default:
                    throw new ApiArgumentException("type");
            }
        }
    }
}

