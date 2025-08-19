using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace FluxoDeCaixaAuth
{
    public class JwtTokenService : ITokenService
    {
        private readonly string _secret;

        public JwtTokenService(string secret)
        {
            _secret = secret ?? throw new ArgumentNullException(nameof(secret));
        }

        public string GenerateToken(string username, IEnumerable<Claim> claims, TimeSpan validFor)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claimsList = new List<Claim>(claims ?? Enumerable.Empty<Claim>());
            claimsList.Add(new Claim(ClaimTypes.Name, username));
            var token = new JwtSecurityToken(claims: claimsList, expires: DateTime.UtcNow.Add(validFor), signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
