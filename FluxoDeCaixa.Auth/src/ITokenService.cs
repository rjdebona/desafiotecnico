using System.Security.Claims;

namespace FluxoDeCaixaAuth
{
    public interface ITokenService
    {
        string GenerateToken(string username, IEnumerable<Claim> claims, TimeSpan validFor);
    }
}
