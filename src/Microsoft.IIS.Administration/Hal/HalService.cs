// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration
{
    using System;
    using Core;
    using System.Dynamic;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using Core.Http;
    using Core.Utils;


    public class HalService : IConditionalHalService
    {
        private const string HAL_KEY = "Microsoft.IIS.Administration.Hal";
        private readonly Dictionary<Guid, SortedList<string, LinkExecutionData>> _links = new Dictionary<Guid, SortedList<string, LinkExecutionData>>();

        public void ProvideLink(Guid resourceId, string name, Func<dynamic, dynamic> func)
        {
            ProvideLink(resourceId, name, func, d => true);
        }

        public void ProvideLink(Guid resourceId, string name, Func<dynamic, dynamic> func, Func<dynamic, bool> condition)
        {
            if (!_links.Keys.Contains(resourceId)) {

                _links[resourceId] = new SortedList<string, LinkExecutionData>();
            }
            _links[resourceId].Add(name, new LinkExecutionData(func, condition));
        }

        public object Apply(Guid resourceId, object obj, bool all = true) {
            if (!IsHalAccepted()) {
                //
                // Don't apply hal
                return obj;
            }

            ExpandoObject o = null;
            if (obj is ExpandoObject) {
                o = (ExpandoObject)obj;
            }
            else {
                o = obj.ToExpando();
            }

            var links = Get(resourceId, o, all);

            if (links.Count() > 0) {
                ((dynamic)o)._links = links;
            }

            SetHalContentType();

            return o;
        }

        public IDictionary<string, dynamic> Get(Guid resourceId, dynamic obj, bool all = true)
        {
            Dictionary<string, dynamic> resourceLinks = new Dictionary<string, dynamic>();

            if (!_links.ContainsKey(resourceId)) {
                return resourceLinks;
            }

            var funcs = _links[resourceId];

            if (!all) {
                var self = funcs.Where(kvp => kvp.Key.Equals("self", StringComparison.OrdinalIgnoreCase));
                if (self.Count() > 0) {
                    var kvp = self.First();
                    if (ConditionalExecuteLink(kvp.Value, obj, out dynamic link)) {
                        resourceLinks.Add(kvp.Key, link);
                    }
                }
            }
            else {
                for (int i = 0; i < funcs.Count; i++) {
                    var kvp = funcs.ElementAt(i);
                    if (ConditionalExecuteLink(kvp.Value, obj, out dynamic link)) {
                        resourceLinks.Add(kvp.Key, link);
                    }
                }
            }

            return resourceLinks;
        }

        private void SetHalContentType()
        {
            if (!HttpHelper.Current.Response.HasStarted && HttpHelper.Current.Response.ContentType == null) {
                HttpHelper.Current.Response.ContentType = HeaderValues.Hal;
            }
        }

        private static bool ConditionalExecuteLink(LinkExecutionData execution, dynamic obj, out dynamic link)
        {
            link = null;
            bool shouldExecute = false;

            try {
                shouldExecute = execution.Condition(obj);

                if (shouldExecute) {
                    link = execution.Create(obj);
                }
            }
            catch (Exception e) {
                link = LinkProvisionError(e.Message);
                return true;
            }

            return shouldExecute;
        }

        private static dynamic LinkProvisionError(string message) {
            return new {
                title = "Link provision error",
                detail = message ?? "",
                status = (int)HttpStatusCode.InternalServerError
            };
        }

        private static bool IsHalAccepted() {
            //
            // The user-agent opt-in for receiving hal
            // by sending http request header
            //
            // Accept: application/hal+json
            //

            var ctx = HttpHelper.Current;
            bool? hal = (bool?) ctx.Items[HAL_KEY];

            if (hal == null) {
                hal = false;

                var acceptHeaders = ctx.Request.Headers[Net.Http.Headers.HeaderNames.Accept];

                foreach (var v in acceptHeaders) {
                    if (v.IndexOf("hal", StringComparison.OrdinalIgnoreCase) > 0) {
                        hal = true;
                        break;
                    }
                }

                ctx.Items[HAL_KEY] = hal;
            }

            return hal.Value;
        }

        class LinkExecutionData
        {
            public LinkExecutionData(Func<dynamic, dynamic> create, Func<dynamic, bool> condition)
            {
                Create = create;
                Condition = condition;
            }

            public Func<dynamic, dynamic> Create { get; private set; }
            public Func<dynamic, bool> Condition { get; private set; }
        }
    }
}
