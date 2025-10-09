using Microsoft.EntityFrameworkCore;
using backendServices.Data;
using backendServices.Models;
using backendServices.AuthController; // for JwtValidator
using System.Threading;

var builder = WebApplication.CreateBuilder(args);

// Users database
var connectionString = builder.Configuration.GetConnectionString("Default");

// HttpClient for Users microservice validation
builder.Services.AddHttpClient();

builder.Services.AddOpenApi();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173") // React app
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

// Wait until Postgres is ready
bool dbConnected = false;
while (!dbConnected)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UtilityDbContext>();
        db.Database.EnsureCreated();
        dbConnected = true;
        Console.WriteLine("Connected to the database successfully.");
    }
    catch
    {
        Console.WriteLine("Waiting for database...");
        Thread.Sleep(2000);
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// app.UseHttpsRedirection();
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


// ✅ Fetch bills
app.MapGet("/fetchUtilityBill", async (HttpContext context, UtilityDbContext db, IHttpClientFactory httpClientFactory) =>
{
    var userId = await JwtValidator.ValidateJwtWithUsersService(context, httpClientFactory);
    if (userId == null) return Results.Unauthorized();

    var bills = await db.Utilities
        .Where(u => u.UserId == userId)
        .Select(u => new
        {
            u.UtilityId,
            u.UserId,
            u.GasUsage,
            u.GasRate,
            u.WaterUsage,
            u.WaterRate,
            u.ElectricityUsage,
            u.ElectricityRate,
            TotalBill = u.TotalBill,
            u.DueDate,
            u.Penalty,
            u.Status
        })
        .ToListAsync();

    return bills.Any()
        ? Results.Ok(bills)
        : Results.NotFound(new { message = "No utility records found for this user" });
});

// ✅ Confirm payment
app.MapPost("/paymentConfirmed", async (HttpContext context, int utilityId, UtilityDbContext db, IHttpClientFactory httpClientFactory) =>
{
    var userId = await JwtValidator.ValidateJwtWithUsersService(context, httpClientFactory);
    if (userId == null) return Results.Unauthorized();

    var utility = await db.Utilities
        .FirstOrDefaultAsync(u => u.UserId == userId && u.UtilityId == utilityId);

    if (utility == null)
        return Results.NotFound(new { message = "No matching utility record found" });

    utility.Status = "paid";
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "Payment confirmed, status updated to paid" });
});

// ✅ Add new utility bill
app.MapPost("/addUtilityBill", async (HttpContext context, Utility newBill, UtilityDbContext db, IHttpClientFactory httpClientFactory) =>
{
    var userId = await JwtValidator.ValidateJwtWithUsersService(context, httpClientFactory);
    if (userId == null) return Results.Unauthorized();

    // force assign UserId from JWT, not from request
    newBill.UserId = userId.Value;
    newBill.Status = "unpaid"; // always start unpaid

    db.Utilities.Add(newBill);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "Utility bill added successfully", billId = newBill.UtilityId });
});

app.Run();
