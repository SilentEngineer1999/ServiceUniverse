using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using HealthApi.Models;

namespace HealthApi.AuthController
{
    public class JwtOptions
    {
        public string? Key { get; set; }         // from appsettings: "Jwt": { "Key": "..." }
        public string? Secret { get; set; }      // optional alias; supported for convenience
        public string? Issuer { get; set; }
        public string? Audience { get; set; }
        public int AccessTokenMinutes { get; set; } = 60;
    }

    public interface IJwtTokenService
    {
        string IssueAccessToken(User u);
    }

    public class JwtTokenService : IJwtTokenService
    {
        private readonly JwtOptions _opt;
        private readonly SymmetricSecurityKey _key;

        public JwtTokenService(IOptions<JwtOptions> opt)
        {
            _opt = opt.Value ?? throw new InvalidOperationException("Jwt options not configured.");

            // Prefer Key; fall back to Secret
            var secret = string.IsNullOrWhiteSpace(_opt.Key) ? _opt.Secret : _opt.Key;
            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("Missing JWT secret. Set Jwt:Key (or Jwt:Secret).");

            // If you store a base64 secret, replace the next line with:
            // var keyBytes = Convert.FromBase64String(secret);
            var keyBytes = Encoding.UTF8.GetBytes(secret);
            _key = new SymmetricSecurityKey(keyBytes);

            if (string.IsNullOrWhiteSpace(_opt.Issuer))   throw new InvalidOperationException("Missing Jwt:Issuer.");
            if (string.IsNullOrWhiteSpace(_opt.Audience)) throw new InvalidOperationException("Missing Jwt:Audience.");
        }

        public string IssueAccessToken(User u)
        {
            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,   u.Id),
                new Claim(JwtRegisteredClaimNames.Email, u.Email ?? string.Empty),
                new Claim(ClaimTypes.Name,               u.Name  ?? string.Empty),
                new Claim(ClaimTypes.Role,               string.IsNullOrWhiteSpace(u.Role) ? "patient" : u.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _opt.Issuer,
                audience: _opt.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(_opt.AccessTokenMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
