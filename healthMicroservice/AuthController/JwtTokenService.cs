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
        public string Issuer { get; set; } = default!;
        public string Audience { get; set; } = default!;
        public string Secret { get; set; } = default!;
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
            _opt = opt.Value;
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Secret));
        }

        public string IssueAccessToken(User u)
        {
            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, u.Id),
                new Claim(JwtRegisteredClaimNames.Email, u.Email),
                new Claim(ClaimTypes.Name, u.Name),
                new Claim(ClaimTypes.Role, u.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _opt.Issuer,
                audience: _opt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_opt.AccessTokenMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
