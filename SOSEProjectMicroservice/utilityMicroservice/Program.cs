using Microsoft.EntityFrameworkCore;
using backendServices.Data;
using backendServices.Models;
using backendServices.AuthController; // for JwtValidator

var builder = WebApplication.CreateBuilder(args);

// DB connection
builder.Services.AddDbContext<UtilityDbContext>(options =>
    options.UseNpgsql("Host=localhost;Port=5432;Database=utilityMicroservices;Username=postgres;Password=root;"));

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

// Ensure DB and tables exist
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UtilityDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

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
