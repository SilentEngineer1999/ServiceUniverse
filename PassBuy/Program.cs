using PassBuy.AuthController;
using PassBuy.Data;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using PassBuy.Models;


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

// Ensure DB + tables exist
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

PasswordManager passwordManager = new();

// POST endpoint to insert a user
app.MapPost("/PassBuy/signUp", async (AppDbContext db, IConfiguration cfg,
    string fname, string lname, string email, string password) =>
{
    try
    {
        var passwordManager = new PasswordManager();
        string hashPassword = passwordManager.HashPassword(password);

        var user = new User
        {
            FirstName = fname,
            LastName  = lname,
            Email     = email,
            Password  = hashPassword
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();  // automatically retrieves and populates user.Id

        var jwt = JwtIssuer.Issue(user.Id, email, cfg);
        return Results.Created($"/users/{user.Id}", new { token = jwt });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.Run();
