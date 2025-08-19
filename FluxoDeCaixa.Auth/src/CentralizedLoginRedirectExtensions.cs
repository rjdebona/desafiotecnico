using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;

namespace FluxoDeCaixaAuth
{
    public class CentralizedLoginRedirectOptions
    {
        public string? LoginBaseUrl { get; set; }
        public Func<HttpContext, bool>? ShouldRedirectPath { get; set; }
        public bool AssumeHtmlWhenNoAccept { get; set; } = true;
    }

    public static class CentralizedLoginRedirectExtensions
    {
        public static IApplicationBuilder UseCentralizedLoginRedirect(this IApplicationBuilder app, Action<CentralizedLoginRedirectOptions>? configure = null)
        {
            var opts = new CentralizedLoginRedirectOptions();
            configure?.Invoke(opts);
            var loginBase = opts.LoginBaseUrl ?? Environment.GetEnvironmentVariable("LOGIN_BASE_URL") ?? "http://localhost:5080";
            if (loginBase.EndsWith('/')) loginBase = loginBase.TrimEnd('/');

            bool DefaultShouldRedirectPath(HttpContext ctx)
            {
                var path = ctx.Request.Path.Value ?? string.Empty;
                var lower = path.ToLowerInvariant();
                if (lower == "/" || lower == "/index" || lower == "/index.html") return true;
                if (lower.StartsWith("/api") || lower.StartsWith("/swagger") || lower.StartsWith("/auth")) return false;
                if (lower.Contains('.') && lower.LastIndexOf('/') < lower.LastIndexOf('.')) return false; // file-like
                return true; // treat as page/SPA route
            }

            bool IsHtmlNavigation(HttpContext ctx)
            {
                if (!HttpMethods.IsGet(ctx.Request.Method)) return false;
                var accept = ctx.Request.Headers["Accept"].ToString();
                var secFetchDest = ctx.Request.Headers["Sec-Fetch-Dest"].ToString();
                bool wantsHtml = (!string.IsNullOrEmpty(accept) && accept.Contains("text/html")) || (string.IsNullOrEmpty(accept) && opts.AssumeHtmlWhenNoAccept);
                bool isDocument = string.Equals(secFetchDest, "document", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(secFetchDest);
                return wantsHtml && isDocument;
            }

            app.Use(async (ctx, next) =>
            {
                var predicate = opts.ShouldRedirectPath ?? DefaultShouldRedirectPath;
                var path = ctx.Request.Path.Value ?? string.Empty;
                var lower = path.ToLowerInvariant();
                bool isRootLike = lower == "/" || lower == "/index" || lower == "/index.html";
                bool hasDot = lower.Contains('.') && lower.LastIndexOf('/') < lower.LastIndexOf('.');
                bool isCandidatePage = !hasDot && !lower.StartsWith("/api") && !lower.StartsWith("/swagger") && !lower.StartsWith("/auth");
                bool unauth = ctx.User?.Identity?.IsAuthenticated != true;
                // Treat any page-like GET (no extension) as navigation when unauthenticated.
                bool shouldRedirectPre = unauth && HttpMethods.IsGet(ctx.Request.Method) && (isRootLike || isCandidatePage) && predicate(ctx);
                if (shouldRedirectPre)
                {
                    var original = ctx.Request.Scheme + "://" + ctx.Request.Host + ctx.Request.Path + ctx.Request.QueryString;
                    ctx.Response.Redirect($"{loginBase}/login?returnUrl={Uri.EscapeDataString(original)}");
                    return;
                }

                await next();

                // Post-response transform: if API returned 401 for what appears to be a page navigation, redirect.
                if (ctx.Response.StatusCode == StatusCodes.Status401Unauthorized && predicate(ctx) && (IsHtmlNavigation(ctx) || isRootLike))
                {
                    var original = ctx.Request.Scheme + "://" + ctx.Request.Host + ctx.Request.Path + ctx.Request.QueryString;
                    ctx.Response.Clear();
                    ctx.Response.Redirect($"{loginBase}/login?returnUrl={Uri.EscapeDataString(original)}");
                }
            });

            return app;
        }
    }
}
