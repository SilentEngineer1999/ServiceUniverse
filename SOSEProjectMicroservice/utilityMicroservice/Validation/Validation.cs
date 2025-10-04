using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Json;

namespace backendServices.AuthController
{
    public static class JwtValidator
    {
        public static async Task<int?> ValidateJwtWithUsersService(HttpContext context, IHttpClientFactory httpClientFactory)
        {
            if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader) || !authHeader.ToString().StartsWith("Bearer "))
                return null;

            var token = authHeader.ToString().Substring("Bearer ".Length).Trim();

            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await client.GetAsync("http://localhost:5238/protected"); // Users microservice URL
                if (!response.IsSuccessStatusCode) return null;
                Console.WriteLine("start");
                var data = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                if (data != null && data.ContainsKey("userId"))
                {
                    Console.WriteLine("finish");
                    return int.Parse(data["userId"]);
                }
            }
            catch
            {
                return null;
            }

            return null;
        }
    }
}
