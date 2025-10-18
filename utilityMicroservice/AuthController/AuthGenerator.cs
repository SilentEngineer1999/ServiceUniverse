using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace backendServices.AuthController
{
    public class AuthGenerator
    {
        public string GenerateJwtToken(string email, int userId, string firstName, string lastName)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ServiceUniverseIsTheSecretString"));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("email", email), // ✅ use explicit key
                new Claim("userId", userId.ToString()),
                new Claim("name", firstName + " " + lastName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };


            var token = new JwtSecurityToken(
                issuer: "serviceUniverse",
                audience: firstName + " " + lastName,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
