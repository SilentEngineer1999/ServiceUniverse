using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace PassBuy.AuthController
{
    public static class JwtValidator
    {
        public static Task<Guid?> ValidateJwtWithUsersService(HttpContext context, IHttpClientFactory? httpClientFactory = null)
        {
            // Check if Authorization header exists
            if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader)
                || !authHeader.ToString().StartsWith("Bearer "))
                return Task.FromResult<Guid?>(null);

            var token = authHeader.ToString().Substring("Bearer ".Length).Trim();

            try
            {
                var principal = ValidateJwt.ValidateJwtToken(token); // 🔹 reuse your local token validator

                var userId = principal.FindFirst("userId")?.Value;
                if (Guid.TryParse(userId, out var parsedId))
                {
                    return Task.FromResult<Guid?>(parsedId);
                }

                return Task.FromResult<Guid?>(null);
            }
            catch
            {
                return Task.FromResult<Guid?>(null);
            }
        }
    }
}
