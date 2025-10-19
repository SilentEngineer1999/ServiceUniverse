using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PassBuy.AuthController
{
    public static class ValidateJwt
    {
        public static ClaimsPrincipal ValidateJwtToken(string token, IConfiguration cfg)
        {
            Console.Write("ClaimsPrincipal");
            var keyString = cfg["Jwt:Key"] ?? throw new InvalidOperationException("Missing Jwt:Key");

            // 👇 VERY IMPORTANT: prevents .NET from renaming claim types
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(keyString);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);

            return principal;
        }
    }
}
