using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace FluxoDeCaixaAuth
{
    public static class AuthServiceCollectionExtensions
    {
    public static IServiceCollection AddFluxoDeCaixaAuth(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSecret = configuration["JWT_SECRET"] ?? Environment.GetEnvironmentVariable("JWT_SECRET") ?? "dev_secret_change_in_prod_please_override_123456"; // >=32 chars
            if (jwtSecret.Length < 32)
            {
                // Pad to minimum 32 chars required for HS256
                jwtSecret = jwtSecret.PadRight(32, '_');
            }
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

            services.AddSingleton<ITokenService>(new JwtTokenService(jwtSecret));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateLifetime = true
                };
                // Allow token in cookie 'fluxo_token' for browser-based login and redirect unauthenticated HTML requests to /login
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (string.IsNullOrEmpty(context.Token))
                        {
                            var cookie = context.Request.Cookies["fluxo_token"];
                            if (!string.IsNullOrEmpty(cookie))
                                context.Token = cookie;
                        }
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        if (!context.Handled)
                        {
                            var req = context.Request;
                            // Detect real document navigation (top-level) vs programmatic fetch.
                            req.Headers.TryGetValue("Accept", out var acceptsRaw);
                            var accepts = acceptsRaw.Count == 0 ? Array.Empty<string>() : acceptsRaw.ToArray();
                            bool wantsHtml = accepts.Any(a => a != null && a.Contains("text/html", StringComparison.OrdinalIgnoreCase));
                            bool wantsJsonExplicit = accepts.Any(a => a != null && a.Contains("application/json", StringComparison.OrdinalIgnoreCase));
                            bool isGet = string.Equals(req.Method, "GET", StringComparison.OrdinalIgnoreCase);
                            // Heuristic: Sec-Fetch-Dest=document indicates a navigation; for fetch/XHR typically 'empty'.
                            req.Headers.TryGetValue("Sec-Fetch-Dest", out var fetchDestRaw);
                            bool isDocument = fetchDestRaw.Count > 0 && fetchDestRaw.Any(d => d != null && d.Equals("document", StringComparison.OrdinalIgnoreCase));
                            // Only redirect if it's a navigation expecting HTML (explicit html OR document dest). Otherwise let 401 bubble for JS to handle.
                            // Se Accept inclui text/html consideramos navegação mesmo que também peça JSON.
                            if (isGet && (wantsHtml || isDocument))
                            {
                                context.HandleResponse();
                                // Preserve original absolute URL (scheme://host + path + query) so after login user returns to original service host
                                var originalUrl = $"{req.Scheme}://{req.Host}{req.Path}{req.QueryString}";
                                var returnUrl = Uri.EscapeDataString(originalUrl);
                                var loginBase = Environment.GetEnvironmentVariable("LOGIN_BASE_URL");
                                if (!string.IsNullOrWhiteSpace(loginBase))
                                {
                                    // Ensure base ends without trailing slash
                                    if (loginBase.EndsWith('/')) loginBase = loginBase.TrimEnd('/');
                                    context.Response.Redirect($"{loginBase}/login?returnUrl={returnUrl}");
                                }
                                else
                                {
                                    context.Response.Redirect($"/login?returnUrl={returnUrl}");
                                }
                            }
                        }
                        return Task.CompletedTask;
                    },
                    OnForbidden = context =>
                    {
                        // Usuário autenticado porém sem autorização para o recurso.
                        var req = context.Request;
                        // Só redireciona em navegação GET de página (evita quebrar chamadas fetch/XHR).
                        if (string.Equals(req.Method, "GET", StringComparison.OrdinalIgnoreCase))
                        {
                            req.Headers.TryGetValue("Accept", out var acceptsRaw);
                            var accepts = acceptsRaw.Count == 0 ? Array.Empty<string>() : acceptsRaw.ToArray();
                            bool wantsHtml = accepts.Any(a => a != null && a.Contains("text/html", StringComparison.OrdinalIgnoreCase));
                            req.Headers.TryGetValue("Sec-Fetch-Dest", out var fetchDestRaw);
                            bool isDocument = fetchDestRaw.Count > 0 && fetchDestRaw.Any(d => d != null && d.Equals("document", StringComparison.OrdinalIgnoreCase));
                            if (wantsHtml || isDocument)
                            {
                                var originalUrl = $"{req.Scheme}://{req.Host}{req.Path}{req.QueryString}";
                                var returnUrl = Uri.EscapeDataString(originalUrl);
                                var loginBase = Environment.GetEnvironmentVariable("LOGIN_BASE_URL");
                                if (!string.IsNullOrWhiteSpace(loginBase))
                                {
                                    if (loginBase.EndsWith('/')) loginBase = loginBase.TrimEnd('/');
                                    context.Response.Redirect($"{loginBase}/login?returnUrl={returnUrl}");
                                }
                                else
                                {
                                    context.Response.Redirect($"/login?returnUrl={returnUrl}");
                                }
                                return Task.CompletedTask;
                            }
                        }
                        // Caso contrário mantém 403 padrão.
                        return Task.CompletedTask;
                    }
                };
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("Consolidator", policy => policy.RequireRole("consolidator"));
            });

            return services;
        }
    }
}
