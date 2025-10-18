using PassBuy.AuthController;
using PassBuy.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using PassBuy.Models;
using System.Globalization;
using System.Reflection.Metadata;


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

// Database pre-processing
using (var scope = app.Services.CreateScope())
{
    // Get the database as a parameter for this scope
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Flag to check if database connected
    bool success = false;

    // Try 10 times to apply migrations if there are any
    Console.WriteLine("Looking for migrations");
    for (var attempt = 1; attempt <= 10 && !success; attempt++)
    {
        try
        {
            // if there are no migrations, will pass gracefully, won't throw
            db.Database.Migrate();
            Console.WriteLine("Migrations successfully applied / or there were no migrations");
            success = true;
        }
        catch (Exception ex) when (attempt < 10)
        {
            Console.WriteLine($"Migration attempt {attempt}: DB not ready yet ({ex.Message}). Retrying in 2s...");
            await Task.Delay(2000);  // wait 2s then try again
        }
    }

    // If it didn't manage to connect after the loop, raise a warning (but it will still run)
    if (!success)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Database failed to connect. No migrations have been applied." +
            "If Migrations exist, please restart the server.");
        Console.ResetColor();
    }
    
    // If the migrations were successful, seed the database with education providers
    else
    {
        var seeder = new DbSeeder(db);
        seeder.SeedEducationProviders();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

PasswordManager passwordManager = new();

// -------------------- SIGN UP --------------------
app.MapPost("/PassBuy/signUp", async (AppDbContext db, IConfiguration cfg,
    string fname, string lname, string email, string password) =>
{
    try
    {
        var (hashPassword, salt) = passwordManager.HashPassword(password);

        var user = new User
        {
            FirstName = fname,
            LastName = lname,
            Email = email,
            Password = hashPassword,
            Salt = salt
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();  // automatically retrieves and populates user.Id

        var token = JwtIssuer.Issue(user.Id, email, cfg);
        return Results.Created($"/users/{user.Id}", new
        { message = "User created successfully.", token });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// -------------------- SIGN IN --------------------
app.MapPost("/PassBuy/signIn", async (AppDbContext db, IConfiguration cfg, string email, string password) =>
{
    try
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return Results.NotFound("User not found");

        bool passwordVerify = passwordManager.VerifyPassword(password, user.Password, user.Salt);
        if (!passwordVerify)
            return Results.Json(new { message = "Incorrect Password" }, statusCode: 401);

        var token = JwtIssuer.Issue(user.Id, email, cfg);
        return Results.Ok(new { message = "User Authenticated", token });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("SignIn");

// I'll forget about this one for now
// app.MapPost("/PassBuy/addDetails", async (AppDbContext db, string bankCard, string mailAddress)) =>
// {

// } 

// ----------------------- ORDER A CARD ------------------

app.MapPost("/PassBuy/newCard/standard", async (AppDbContext db, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var userId = await JwtValidator.ValidateJwtWithUsersService(context, httpClientFactory);
    if (userId == null) return Results.Unauthorized();

    try
    {
        var newCard = new PassBuyCardApplication {
            UserId = userId.Value,
            CardType = CardType.Standard
        };
        db.PassBuyCardApplications.Add(newCard);
        await db.SaveChangesAsync();

        return Results.Created(
            new { message = "New PassBuy Concession Application submitted. Class: Standard" }
            );
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("newCard/standard");

app.MapPost("/PassBuy/newCard/education", async (AppDbContext db, HttpContext context, IHttpClientFactory httpClientFactory,
            string eduCode, int stuNum, int courseCode, string courseTitle ) =>
{
    var userId = await JwtValidator.ValidateJwtWithUsersService(context, httpClientFactory);
    if (userId == null) return Results.Unauthorized();

    // begin transaction (with multiple steps)
    await using var tx = await db.Database.BeginTransactionAsync();

    try
    {
        var newCard = new PassBuyCardApplication {
            UserId = userId.Value,
            CardType = CardType.EducationConcession
        };
        await db.SaveChangesAsync();

        // Find education provider
        var provider = await db.EducationProviders.FirstOrDefaultAsync(e => e.EduCode == eduCode);
        if (provider is null) return Results.NotFound("University not found"); //404

        // Make Education Details object
        var eduDetails = new EducationDetails
        {
            ApplicationId = newCard.Id,
            ProviderId = provider.Id,
            StudentNumber = stuNum,
            CourseCode = courseCode,
            CourseTitle = courseTitle
        };

        // Add education details to the card
        newCard.EducationDetails = eduDetails;

        // Adding newCard adds EducationDetails to the database as well
        db.PassBuyCardApplications.Add(newCard);
        await db.SaveChangesAsync();

        // Commit the transaction
        await tx.CommitAsync();

        return Results.Created(
            new { message = "New PassBuy Concession Application submitted. Class: Education" }
            );
    }
    catch (Exception ex)
    {
        await tx.RollbackAsync(); // Rollback transaction
        return Results.Problem(ex.Message);
    }
})
.WithName("newCard/education");


// app,MapPost("/PassBuy/checkout", async (AppDbContext db, HttpContext context, IHttpClientFactory httpClientFactory,
//             string bankCard, int startingBalance, string mailAddress)) =>
// {
    
// }

app.Run();