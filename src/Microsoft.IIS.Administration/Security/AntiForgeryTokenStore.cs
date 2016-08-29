// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration {
    using System;
    using System.Threading.Tasks;
    using AspNetCore.Antiforgery;
    using AspNetCore.Http;
    using AspNetCore.Antiforgery.Internal;
    using Extensions.Options;

    public class AntiForgeryTokenStore : IAntiforgeryTokenStore {
        private readonly IAntiforgeryTokenStore _defaultStore;
        private readonly AntiforgeryOptions _options;
        private readonly IAntiforgeryTokenSerializer _tokenSerializer;


        public AntiForgeryTokenStore(IOptions<AntiforgeryOptions> optionsAccessor, IAntiforgeryTokenSerializer tokenSerializer) {
            _defaultStore = new DefaultAntiforgeryTokenStore(optionsAccessor);
            _options = optionsAccessor.Value;
            _tokenSerializer = tokenSerializer;
        }

        public string GetCookieToken(HttpContext httpContext) {
            // Default implementation
            return _defaultStore.GetCookieToken(httpContext);
        }

        public async Task<AntiforgeryTokenSet> GetRequestTokensAsync(HttpContext httpContext) {
            //
            // Get cookie token
            string requestCookie = httpContext.Request.Cookies[_options.CookieName];
            string requestToken = null;

            //
            // Get header token
            if (!string.IsNullOrEmpty(requestCookie)) {
                requestToken = httpContext.Request.Headers[_options.FormFieldName];
            }

            //
            if (!string.IsNullOrEmpty(requestToken)) {
                return new AntiforgeryTokenSet(requestToken, requestCookie, _options.CookieName, _options.FormFieldName);
            }

            //
            // Fall back to the default implementation
            try {
                var res = await _defaultStore.GetRequestTokensAsync(httpContext);
                return res;
            }
            catch (Exception e) {
                throw new AntiforgeryException(e);
            }
        }

        public void SaveCookieToken(HttpContext httpContext, string token) {
            if (httpContext == null) {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (token == null) {
                throw new ArgumentNullException(nameof(token));
            }

            // Add the cookie to the request based context.
            // This is useful if the cookie needs to be reloaded in the context of the same request.

            var services = httpContext.RequestServices;
            //var contextAccessor = services.GetRequiredService<antifor>();
            
            //contextAccessor.Value = new AntiforgeryContext() { CookieToken = token };

            var options = new CookieOptions() { HttpOnly = true };

            // Note: don't use "newCookie.Secure = _options.RequireSSL;" since the default
            // value of newCookie.Secure is populated out of band.
            if (_options.RequireSsl) {
                options.Secure = true;
            }

            options.Path = httpContext.Request.Path;

            httpContext.Response.Cookies.Append(_options.CookieName, token, options);
        }
    }
}
