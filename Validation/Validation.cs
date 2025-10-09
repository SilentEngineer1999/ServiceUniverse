using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace backendServices.AuthController
{
    public static class JwtValidator
    {
        public static Task<int?> ValidateJwtWithUsersService(HttpContext context, IHttpClientFactory? httpClientFactory = null)
        {
            // Check if Authorization header exists
            if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader)
                || !authHeader.ToString().StartsWith("Bearer "))
                return Task.FromResult<int?>(null);

            var token = authHeader.ToString().Substring("Bearer ".Length).Trim();

            try
            {
                var principal = ValidateJwt.ValidateJwtToken(token); // 🔹 reuse your local token validator

                var userId = principal.FindFirst("userId")?.Value;
                if (int.TryParse(userId, out var parsedId))
                {
                    return Task.FromResult<int?>(parsedId);
                }

                return Task.FromResult<int?>(null);
            }
            catch
            {
                return Task.FromResult<int?>(null);
            }
        }
    }
}
