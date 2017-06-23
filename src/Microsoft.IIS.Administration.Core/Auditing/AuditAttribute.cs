// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core
{
    using AspNetCore.Http;
    using AspNetCore.Mvc.Filters;
    using Newtonsoft.Json.Linq;
    using Security;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Text;
    using System.Threading.Tasks;

    public class AuditAttribute : ActionFilterAttribute
    {
        public const string ALL = "*";
        public static ILogger Logger { get; set; }

        private const string AUDIT_TARGET = "IIS.ADMIN.AUDIT";
        private const string HIDDEN_VALUE = "{HIDDEN}";
        private static readonly string[] SENSITIVE_KEYWORDS = new string[] { "private", "password", "key", "hash", "secret" };

        // 2D array because we store fields as arrays of segments, i.e. model.identity.password -> ["model", "identity", "password"]
        private string[][] _enabledFields;
        private string[][] _hiddenFields;
        private IEnumerable<string> _globalNonsensitiveFields;
        private bool _allFieldsEnabled = false;

        public AuditAttribute(string fields = "*", string maskedFields = null)
        {
            _enabledFields = MakeFields(fields);
            _hiddenFields = MakeFields(maskedFields);

            // Remove fields that are present in hidden fields to avoid unnecessary processing
            _enabledFields = RemoveIntersection(_enabledFields, _hiddenFields);

            // Check if user wants to enable auditing of all fields
            foreach (string[] field in _enabledFields) {
                if (field[0].Equals(ALL)) {
                    _allFieldsEnabled = true;
                }
            }
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (IsSuccess(context.HttpContext.Response.StatusCode) && GetArgs(context.HttpContext).Count > 0) {
                string apiKeyId = GetRequestApiKeyId(context.HttpContext);

                var sb = StartAudit(apiKeyId, context.HttpContext.Request.Method, context.HttpContext.Request.Path);

                // Iterate over the action arguments we stored in the Http Context
                foreach (var kvp in GetArgs(context.HttpContext)) {
                    // Clear any necessary hidden field values for the argument
                    var cachedState = ClearHiddenFields(kvp.Key, kvp.Value);

                    AppendArg(kvp.Key, kvp.Value, sb);

                    if (cachedState != null) {
                        // Restore any hidden fields to their original state
                        RestoreHiddenFields(kvp.Key, kvp.Value, cachedState);
                    }
                }
                FinishAudit(sb);
            }

            ClearArgs(context.HttpContext);

            base.OnActionExecuted(context);
        }

        public override void OnActionExecuting(ActionExecutingContext actionContext)
        {
            if (_globalNonsensitiveFields == null) {
                var nonsensitiveFields = (INonsensitiveAuditingFields)actionContext.HttpContext.RequestServices.GetService(typeof(INonsensitiveAuditingFields));
                _globalNonsensitiveFields = nonsensitiveFields?.Value ?? Enumerable.Empty<string>();
            }

            // Store action arguments in the http context to be accessed later and logged if the request was successful
            foreach (var arg in actionContext.ActionArguments.Keys) {
                foreach (string[] hf in _hiddenFields) {
                    if (hf.Length == 1 && hf[0].Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                        // Don't add hidden action argument
                        continue;
                    }
                }

                if (!_allFieldsEnabled) {
                    foreach (string[] field in _enabledFields) {
                        if (field[0].Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                            if (field.Length > 1) {
                                // Handle adding field with multiple segments like model.identity.cpu
                                var target = actionContext.ActionArguments[arg] as JToken;
                                if (target == null) {
                                    // Adding granular properties is only supported for JObject
                                    break;
                                }

                                // Navigate through the object to the field that is targetted for auditing
                                for (int i = 1; i < field.Length; i++) {
                                    target = target[field[i]];
                                }

                                AddArg(actionContext.HttpContext, string.Join(".", field), target);
                            }
                            else {
                                AddArg(actionContext.HttpContext, arg, actionContext.ActionArguments[arg]);
                            }
                        }
                    }
                }
                else {
                    AddArg(actionContext.HttpContext, arg, actionContext.ActionArguments[arg]);
                }
            }

            base.OnActionExecuting(actionContext);
        }

        private IList<KeyValuePair<string[], JToken>> ClearHiddenFields(string argName, dynamic value)
        {
            IList<KeyValuePair<string[], JToken>> cachedState = new List<KeyValuePair<string[], JToken>>();

            // Granular hidden fields only supported for JObject
            JObject v = value as JObject;
            if (v == null) {
                return null;
            }

            // Always remove _links if applicable, only for the root object
            var t = RemoveLinks(v);
            if (t != null) {
                cachedState.Add(new KeyValuePair<string[], JToken>($"{argName}._links".Split('.'), t));
            }

            string[] argNameSegs = argName.Split('.');

            // Check if hidden field is our field name and more i.e. field: model.identity | hidden field: model.identity.password
            foreach (string[] hf in _hiddenFields) {
                bool match = hf.Length > argNameSegs.Length;
                if (!match) {
                    continue;
                }

                int i;
                for (i = 0; i < argNameSegs.Length; i++) {
                    if (!hf[i].Equals(argNameSegs[i], StringComparison.OrdinalIgnoreCase)) {
                        match = false;
                        break;
                    }
                }
                if (!match) {
                    continue;
                }

                JToken seeker = v;

                // i already at argNameSegs.length - 1;
                for (; i < hf.Length - 1; i++) {
                    if (seeker == null) {
                        break;
                    }

                    seeker = seeker[hf[i]];
                }

                if (seeker == null) {
                    continue;
                }

                cachedState.Add(new KeyValuePair<string[], JToken>(hf, seeker[hf[hf.Length - 1]]));
                seeker[hf[hf.Length - 1]] = HIDDEN_VALUE;
            }

#if DEBUG
            // Ensure that fields with sensitive keywords are addressed explicitly
            AssertSensitiveFieldsHidden(v, argName);
#endif
            return cachedState;
        }

        private void RestoreHiddenFields(string argName, dynamic value, IList<KeyValuePair<string[], JToken>> cachedState)
        {
            // Granular hidden fields only supported for JObject
            JObject v = value as JObject;
            if (v == null) {
                return;
            }

            string[] argNameSegs = argName.Split('.');

            foreach (var kvp in cachedState) {
                JToken seeker = v;
                string[] fieldSegs = kvp.Key;

                // Position the seeker to the property we are trying to restore. 
                // i.e. to restore identity.data.password set seeker to the 'data' object
                for (int i = argNameSegs.Length; i < fieldSegs.Length - 1; i++) {
                    if (seeker == null) {
                        break;
                    }
                    seeker = seeker[fieldSegs[i]];
                }

                // Index into the seeker to restore the cached value
                if (seeker != null) {
                    seeker[fieldSegs[fieldSegs.Length - 1]] = kvp.Value;
                }
            }
        }

        private StringBuilder StartAudit(string apiKeyId, string method, string path)
        {
            StringBuilder sb = new StringBuilder();

            var nl = System.Environment.NewLine;
            sb.Append($"{nl}API Key ID: {apiKeyId}{nl}Method: {method}{nl}Path: {path}{nl}");

            return sb;
        }

        private void AppendArg(string argName, dynamic argValue, StringBuilder sb)
        {
            if (sb == null) {
                throw new ArgumentNullException(nameof(sb));
            }

            sb.Append($"{argName}: {argValue.ToString()}{System.Environment.NewLine}");
        }

        private void FinishAudit(StringBuilder sb)
        {
            sb.Append(System.Environment.NewLine);

            if (Logger != null) {
                Task.Run(() => {
                    Logger.Information(sb.ToString());
                });
            }
        }

        private bool IsSuccess(int statusCode)
        {
            return (statusCode >= 200 && statusCode < 400);
        }

        //
        // The arguments that are targetted by auditing are stored in the Http Context Items store between the action executing and when the action finishes executing
        // These are helpers to interact with where we put the arguments
        //
        private void AddArg(HttpContext context, string argName, object val)
        {
            if (!context.Items.ContainsKey(AUDIT_TARGET)) {
                context.Items[AUDIT_TARGET] = new Dictionary<string, object>();
            }

            ((Dictionary<string, object>)context.Items[AUDIT_TARGET])[argName] = val;
        }

        private Dictionary<string, object> GetArgs(HttpContext context)
        {
            if (!context.Items.ContainsKey(AUDIT_TARGET)) {
                context.Items[AUDIT_TARGET] = new Dictionary<string, object>();
            }

            return ((Dictionary<string, object>)context.Items[AUDIT_TARGET]);
        }

        private void ClearArgs(HttpContext context)
        {
            context.Items[AUDIT_TARGET] = null;
        }

        private string[][] MakeFields(string input)
        {
            string[][] fields;

            if (input != null) {
                var withPeriods = input.Split(',');
                fields = new string[withPeriods.Length][];

                for (int i = 0; i < withPeriods.Length; i++) {
                    withPeriods[i] = withPeriods[i].Trim();
                    fields[i] = withPeriods[i].Split('.');
                }
            }
            else {
                fields = new string[0][];
            }

            return fields;
        }

        // enabledFields minus hiddenFields
        private string[][] RemoveIntersection(string[][] enabledFields, string[][] hiddenFields)
        {
            List<string[]> dissection = new List<string[]>();

            foreach (string[] field in _enabledFields) {
                // Keep track of whether we want to keep this enabled field
                bool addBack = true;

                foreach (string[] hiddenField in _hiddenFields) {
                    if (hiddenField.Length != field.Length) {
                        continue;
                    }

                    int i = 0;
                    for (i = 0; i < hiddenField.Length; i++) {
                        if (!hiddenField[i].Equals(field[i], StringComparison.OrdinalIgnoreCase)) {
                            break;
                        }
                    }

                    // If for loop checking equivalence of the field array got to the end addBack is false
                    addBack = i != hiddenField.Length;
                }

                if (addBack) {
                    dissection.Add(field);
                }
            }

            return dissection.ToArray();
        }

        private void AssertSensitiveFieldsHidden(JObject model, string parentJPath)
        {
            if (model == null) {
                return;
            }

            foreach (JProperty property in model.Properties()) {

                JObject jChild = property.Value as JObject;
                if (jChild != null) {
                    AssertSensitiveFieldsHidden(jChild, parentJPath + "." + property.Name);
                    continue;
                }

                bool containsSensitiveKeyword = SENSITIVE_KEYWORDS.Any(keyword => property.Name.ToLowerInvariant().Contains(keyword));

                string field = parentJPath + "." + property.Name;
                if (containsSensitiveKeyword
                        && (property.Value.Type != JTokenType.String || property.Value.Value<string>() != HIDDEN_VALUE)
                        && !(_enabledFields.Any(f => string.Join(".", f).Equals(field, StringComparison.OrdinalIgnoreCase)))
                        && !_globalNonsensitiveFields.Any(f => field.EndsWith(f, StringComparison.OrdinalIgnoreCase))) {
                    throw new Exception("Possibly sensitive field not marked hidden or explicitly enabled");
                }
            }
        }

        private JToken RemoveLinks(JObject o)
        {
            JToken jToken = null;

            if(o["_links"] != null)
            {
                jToken = o["_links"];
                o["_links"] = null;
            }

            return jToken;
        }

        private string GetRequestApiKeyId(HttpContext context)
        {
            var principal = context.User as ClaimsPrincipal;

            if (principal == null)
            {
                return null;
            }

            Claim tokenClaim = principal.Claims.Where(c => c.Type == Core.Security.ClaimTypes.AccessToken).FirstOrDefault();
            IApiKeyProvider keyProvider = (IApiKeyProvider)context.RequestServices.GetService(typeof(IApiKeyProvider));

            if (tokenClaim == null || keyProvider == null)
            {
                return null;
            }

            var requestKey = keyProvider.FindKey(tokenClaim.Value);

            return requestKey == null ? null : requestKey.Id;
        }
    }
}
