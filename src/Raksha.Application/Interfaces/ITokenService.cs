using System.Security.Claims;

namespace Raksha.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(Guid userId, string email, string userName, IList<string> roles);
        string GenerateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}
