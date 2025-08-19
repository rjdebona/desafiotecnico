using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FluxoDeCaixaAuth.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _config;
        public AuthController(ITokenService tokenService, IConfiguration config)
        {
            _tokenService = tokenService;
            _config = config;
        }

        public record LoginRequest(string Username, string Password);

        [HttpPost("token")]
        [AllowAnonymous]
        public IActionResult Token([FromBody] LoginRequest request)
        {
            var adminUser = _config["ADMIN_USER"] ?? Environment.GetEnvironmentVariable("ADMIN_USER") ?? "admin";
            var adminPass = _config["ADMIN_PASS"] ?? Environment.GetEnvironmentVariable("ADMIN_PASS") ?? "password";
            if (request is null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { error = "Credenciais inválidas" });
            if (!string.Equals(request.Username, adminUser, StringComparison.Ordinal) || !string.Equals(request.Password, adminPass, StringComparison.Ordinal))
                return Unauthorized(new { error = "Credenciais inválidas" });

            var claims = new List<Claim>();
            // Grant consolidator role to the single admin user for now
            claims.Add(new Claim(ClaimTypes.Role, "consolidator"));
            var token = _tokenService.GenerateToken(request.Username, claims, TimeSpan.FromHours(8));

            Response.Cookies.Append("fluxo_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddHours(8)
            });
            return Ok(new { access_token = token });
        }

        [HttpPost("logout")]
        [HttpGet("logout")]
        public IActionResult Logout()
        {
            if (Request.Cookies.ContainsKey("fluxo_token"))
            {
                Response.Cookies.Append("fluxo_token", "", new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(-1),
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax
                });
            }
            if (Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                var returnUrl = Request.Query["returnUrl"].FirstOrDefault() ?? "/";
                return Redirect(returnUrl);
            }
            return Ok();
        }
    }
}
