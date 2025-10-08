using PassBuy.AuthController;
using PassBuy.Data;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Default");
var cfg = builder.Configuration;

// JWT variables
var jwtKey      = cfg["Jwt:Key"]      ?? throw new InvalidOperationException("Missing Jwt:Key");
var jwtIssuer   = cfg["Jwt:Issuer"]   ?? "PassBuy";
var jwtAudience = cfg["Jwt:Audience"] ?? "PassBuyClients";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true, ValidateAudience = true,
            ValidateLifetime = true, ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer, ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(30) // small leeway
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

PasswordManager passwordManager = new();

// POST endpoint to insert a user
app.MapPost("/signUp", async (string fname, string lname, int age, string email, string password) =>
{
    try
    {
        string hashPassword = passwordManager.HashPassword(password);
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "INSERT INTO users (firstName, lastName, email, password) VALUES (@firstName, @lastName, @email, @password)", conn);

        cmd.Parameters.AddWithValue("firstName", fname);
        cmd.Parameters.AddWithValue("lastName", lname);
        cmd.Parameters.AddWithValue("email", email);
        cmd.Parameters.AddWithValue("password", hashPassword);

        await cmd.ExecuteNonQueryAsync();

        var jwt = JwtIssuer.Issue(newUserId, email, cfg);

        // Return token (and maybe the user id)
        return Results.Created($"/users/{newUserId}", new { token = jwt});

    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapGet("/signIn", async (string email, string password) =>
{
    try
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand("SELECT id, firstName, lastName, email, password FROM users WHERE email = @Email", conn);
        cmd.Parameters.AddWithValue("Email", email);
        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var user = new
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                FirstName = reader.GetString(reader.GetOrdinal("firstName")),
                LastName = reader.GetString(reader.GetOrdinal("lastName")),
                Email = reader.GetString(reader.GetOrdinal("email")),
                Password = reader.GetString(reader.GetOrdinal("password"))
            };

            bool passwordVerify = passwordManager.VerifyPassword(password, user.Password);
            if (passwordVerify)
            {
                return Results.Ok("User Authenticated");
            }
            else
            {
                return Results.Json(new { message = "Incorrect Password" }, statusCode: 401);
            }
        }
        else
        {
            return Results.NotFound("User not found");
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("SignIn");

app.Run();
