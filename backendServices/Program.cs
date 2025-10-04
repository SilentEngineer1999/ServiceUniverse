using backendServices.AuthController;
using backendServices.Data;
using backendServices.Models;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);
var connectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=root;";

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddOpenApi();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins(
                "http://localhost:5173", // React app
                "http://localhost:5104"  // Utility microservice
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
        });
});


var app = builder.Build();

// Ensure DB + tables exist
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

PasswordManager passwordManager = new();
AuthGenerator authGenerator = new();

// -------------------- SIGN UP --------------------
app.MapPost("/signUp", async (AppDbContext db, string fname, string lname, int age, string email, string password) =>
{
    try
    {
        var (hashPassword, salt) = passwordManager.HashPassword(password);

        var user = new User
        {
            FirstName = fname,
            LastName = lname,
            Age = age,
            Email = email,
            Password = hashPassword,
            Salt = salt
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var token = authGenerator.GenerateJwtToken(email, user.Id, fname, lname);

        return Results.Ok(new { message = "User inserted successfully", token });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// -------------------- SIGN IN --------------------
app.MapPost("/signIn", async (AppDbContext db, string email, string password) =>
{
    try
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return Results.NotFound("User not found");

        bool passwordVerify = passwordManager.VerifyPassword(password, user.Password, user.Salt);
        if (!passwordVerify)
            return Results.Json(new { message = "Incorrect Password" }, statusCode: 401);

        var token = authGenerator.GenerateJwtToken(email, user.Id, user.FirstName, user.LastName);
        return Results.Ok(new { message = "User Authenticated", token });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("SignIn");

// -------------------- PROTECTED --------------------
app.MapGet("/protected", (HttpContext context) =>
{
    // 1️⃣ Get Authorization header
    if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader)
        || !authHeader.ToString().StartsWith("Bearer "))
        return Results.Unauthorized();

    var token = authHeader.ToString().Substring("Bearer ".Length).Trim();

    try
    {
        // 2️⃣ Validate JWT and skip issuer/audience checks
        var principal = ValidateJwt.ValidateJwtToken(token);
        // 3️⃣ Extract claims
        var userId = principal.FindFirst("userId")?.Value;
        var name = principal.FindFirst("name")?.Value;
        var email = principal.FindFirst("email")?.Value;

        // 4️⃣ Return all claims
        return Results.Ok(new { message = "Valid token", userId, name, email });
    }
    catch
    {
        return Results.Unauthorized();
    }
});



app.Run();
