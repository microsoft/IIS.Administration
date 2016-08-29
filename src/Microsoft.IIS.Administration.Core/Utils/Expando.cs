// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core.Utils {
    using System.Collections.Generic;
    using System.Dynamic;
    using AspNetCore.Routing;

    public static class Expando {
        public static ExpandoObject ToExpando(this object anonymousObject) {
            IDictionary<string, object> dict = new RouteValueDictionary(anonymousObject);
            IDictionary<string, object> expando = new ExpandoObject();

            foreach (var item in dict) {
                expando.Add(item);
            }

            return (ExpandoObject)expando;
        }
    }
}
