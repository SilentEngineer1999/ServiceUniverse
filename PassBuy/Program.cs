using PassBuy.AuthController;
using PassBuy.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using PassBuy.Models;
using System.Globalization;
using System.Reflection.Metadata;
using System.Security.Cryptography.X509Certificates;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var cfg = builder.Configuration;

// JWT variables
var jwtKey      = cfg["Jwt:Key"]      ?? throw new InvalidOperationException("Missing Jwt:Key");
var jwtIssuer   = cfg["Jwt:Issuer"]   ?? "ServiceUniverse";
var jwtAudience = cfg["Jwt:Audience"] ?? "PassBuyClients";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(30) // small leeway
        };
    });

builder.Services.AddAuthorization();

// HttpClient for authentication validation
builder.Services.AddHttpClient();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
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
                  // .AllowCredentials() // not needed for bearer tokens; enable only if using cookies
        });
});

var app = builder.Build();

// Database pre-processing
using (var scope = app.Services.CreateScope())
{
    // Get the database
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
        catch (Exception ex)
        {
            Console.WriteLine($"Migration attempt {attempt}: DB not ready yet ({ex.Message}). Retrying in 2s...");
            success = false;
            await Task.Delay(2000);  // wait 2s then try again
        }
    }

    // If it didn't manage to connect after the loop, raise a warning (but it will still run)
    if (!success)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Database failed to connect. No migrations have been applied." +
            " If Migrations exist, please restart the server.");
        Console.ResetColor();
    }
    else
    {
        DbSeeder.SeedEducationProviders(db);
        DbSeeder.SeedTransportEmployers(db);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // In dev, skip HTTPS redirection to avoid CORS+redirect header issues
}
else
{
    app.UseHttpsRedirection();
}

// ---- Enable CORS for the frontend (must be before endpoints) ----
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

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

        // Return 201 Created (as requested)
        return Results.Created($"/PassBuy/sessions/{Guid.NewGuid()}",
            new { message = "User Authenticated", token });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("SignIn");

// ----------------------- ORDER A CARD ------------------

app.MapPost("/PassBuy/newCard/standard", async (AppDbContext db, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var authHeader = context.Request.Headers["Authorization"].ToString();
    if (!authHeader.StartsWith("Bearer "))
        return Results.Unauthorized();

    var token = authHeader["Bearer ".Length..].Trim();
    var principal = ValidateJwt.ValidateJwtToken(token, cfg);

    var userIdString = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    if (!Guid.TryParse(userIdString, out var userId))
        return Results.Unauthorized();

    try
    {
        var newCard = new PassBuyCardApplication {
            UserId = userId,
            CardType = CardType.Standard
        };
        db.PassBuyCardApplications.Add(newCard);
        await db.SaveChangesAsync();

        return Results.Created(
            uri: (string?) null,
            value: new { message = "New PassBuy Card application submitted. Class: Standard" }
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
    var authHeader = context.Request.Headers["Authorization"].ToString();
    if (!authHeader.StartsWith("Bearer "))
        return Results.Unauthorized();

    var token = authHeader["Bearer ".Length..].Trim();
    var principal = ValidateJwt.ValidateJwtToken(token, cfg);

    var userIdString = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    if (!Guid.TryParse(userIdString, out var userId))
        return Results.Unauthorized();

    // begin transaction (with multiple steps)
    await using var tx = await db.Database.BeginTransactionAsync();

    try
    {
        var newCard = new PassBuyCardApplication {
            UserId = userId,
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

        // This is where Education Details would be sent
        // to the Educational Institution for Verification

        return Results.Created(
            uri: (string?) null,
            value: new { message = "New PassBuy Concession Application approved! " +
            "Class: Education" }
            );
    }
    catch (Exception ex)
    {
        await tx.RollbackAsync(); // Rollback transaction
        return Results.Problem(ex.Message);
    }
})
.WithName("newCard/education");

app.MapPost("/PassBuy/newCard/transportEmployee", async (AppDbContext db, HttpContext context, IHttpClientFactory httpClientFactory,
            string employer, int employeeNumber ) =>
{
    var authHeader = context.Request.Headers["Authorization"].ToString();
    if (!authHeader.StartsWith("Bearer "))
        return Results.Unauthorized();

    var token = authHeader["Bearer ".Length..].Trim();
    var principal = ValidateJwt.ValidateJwtToken(token, cfg);

    var userIdString = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    if (!Guid.TryParse(userIdString, out var userId))
        return Results.Unauthorized();

    // begin transaction (with multiple steps)
    await using var tx = await db.Database.BeginTransactionAsync();

    try
    {
        var newCard = new PassBuyCardApplication {
            UserId = userId,
            CardType = CardType.TransportEmployeeConcession
        };
        await db.SaveChangesAsync();

        // Find education provider
        var transportEmployer = await db.TransportEmployers.FirstOrDefaultAsync(e => e.Name == employer);
        if (transportEmployer is null) return Results.NotFound("Transport Employer not found"); //404

        // Make Transport Employee Details object
        var employeeDetails = new TransportEmploymentDetails
        {
            ApplicationId = newCard.Id,
            EmployerId = transportEmployer.Id,
            EmployeeNumber = employeeNumber
        };

        // Add education details to the card
        newCard.TransportEmploymentDetails = employeeDetails;

        // Adding newCard adds EducationDetails to the database as well
        db.PassBuyCardApplications.Add(newCard);
        await db.SaveChangesAsync();

        // Commit the transaction
        await tx.CommitAsync();

        // This is where Transport Employment Details would be sent
        // to the employer for verification

        return Results.Created(
            uri: (string?) null,
            value: new { message = "New PassBuy Concession Application approved! " + 
                    "Class: Transport Employee" }
            );
    }
    catch (Exception ex)
    {
        await tx.RollbackAsync(); // Rollback transaction
        return Results.Problem(ex.Message);
    }
})
.WithName("newCard/transportEmployee");

app.MapPost("/PassBuy/newCard/concession", async (AppDbContext db, HttpContext context, IHttpClientFactory httpClientFactory,
            DateTime DoB, string fullLegalName, int cardType) =>
{
    var authHeader = context.Request.Headers["Authorization"].ToString();
    if (!authHeader.StartsWith("Bearer "))
        return Results.Unauthorized();

    var token = authHeader["Bearer ".Length..].Trim();
    var principal = ValidateJwt.ValidateJwtToken(token, cfg);

    var userIdString = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    if (!Guid.TryParse(userIdString, out var userId))
        return Results.Unauthorized();

    try
    {
        var newCard = new PassBuyCardApplication
        {
            UserId = userId,
            CardType = (CardType)cardType
        };
        await db.SaveChangesAsync();

        // Here is where GovID Details would be verified with Government Documents service
        // and returned

        return Results.Created(
            uri: (string?)null,
            value: new
            {
                message = "New PassBuy Concession Application approved! " +
                    $"Class: {(CardType)cardType}"
            }
            );
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("newCard/concession");

// Fulfillment
// Approves user's most recent Pending application (or a specific one if provided)
// and issues a PassBuyCard linked to it.
app.MapPost("/PassBuy/fulfilment", async (
    AppDbContext db,
    HttpContext context,
    int? applicationId,
    string address,
    string city,
    string state,
    string postcode,
    string country,
    string? topupMode,
    decimal? autoThreshold,
    decimal? autoAmount,
    string? scheduleCadence,
    decimal? scheduleAmount,
    string? bankAccountId,
    string? bankAccount
) =>
{
    var authHeader = context.Request.Headers["Authorization"].ToString();
    if (!authHeader.StartsWith("Bearer ")) return Results.Unauthorized();

    var token = authHeader["Bearer ".Length..].Trim();
    var principal = ValidateJwt.ValidateJwtToken(token, cfg);

    var userIdString = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    if (!Guid.TryParse(userIdString, out var userId)) return Results.Unauthorized();

    var bankValue = bankAccount ?? bankAccountId;
    if (string.IsNullOrWhiteSpace(bankValue))
        return Results.BadRequest("Bank account is required.");

    var query = db.PassBuyCardApplications
        .Where(a => a.UserId == userId && a.status == "Pending");

    if (applicationId.HasValue)
        query = query.Where(a => a.Id == applicationId.Value);

    var appRow = await query.OrderByDescending(a => a.DateApplied).FirstOrDefaultAsync();
    if (appRow is null) return Results.NotFound("No pending application found to fulfil.");

    appRow.status = "Approved";

    var mode = string.IsNullOrWhiteSpace(topupMode) ? "manual" : topupMode.Trim().ToLowerInvariant();
    decimal? amount = mode == "auto" ? autoAmount
                      : mode == "scheduled" ? scheduleAmount
                      : null;

    var card = new PassBuyCard
    {
        UserId = userId,
        CardType = appRow.CardType,
        Application = appRow,
        TopUpMode = mode,
        AutoThreshold = mode == "auto" ? autoThreshold : null,
        TopUpAmount = amount,
        TopUpSchedule = mode == "scheduled" ? scheduleCadence : null,
        BankAccount = bankValue
    };

    db.PassBuyCards.Add(card);
    await db.SaveChangesAsync();

    return Results.Created($"/PassBuy/cards/{card.Id}", new
    {
        message = "Application approved and card issued.",
        cardId = card.Id,
        applicationId = appRow.Id,
        cardType = appRow.CardType.ToString(),
        topUp = new
        {
            mode = card.TopUpMode,
            threshold = card.AutoThreshold,
            amount = card.TopUpAmount,
            schedule = card.TopUpSchedule,
            bank = card.BankAccount
        }
    });
})
.WithName("PassBuyFulfilment");


// List cards
app.MapGet("/PassBuy/cards", async (AppDbContext db, HttpContext context) =>
{
    var authHeader = context.Request.Headers["Authorization"].ToString();
    if (!authHeader.StartsWith("Bearer ")) return Results.Unauthorized();

    var token = authHeader["Bearer ".Length..].Trim();
    var principal = ValidateJwt.ValidateJwtToken(token, cfg);

    var userIdString = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    if (!Guid.TryParse(userIdString, out var userId)) return Results.Unauthorized();

    var cards = await db.PassBuyCards
        .Where(c => c.UserId == userId)
        .OrderByDescending(c => c.DateApproved)
        .Select(c => new
        {
            id = c.Id,
            userId = c.UserId,
            cardType = c.CardType.ToString(),
            dateApproved = c.DateApproved,
            topUpMode = c.TopUpMode,
            autoThreshold = c.AutoThreshold,
            topUpAmount = c.TopUpAmount,
            topUpSchedule = c.TopUpSchedule,
            bankAccount = c.BankAccount,
            applicationId = c.Application != null ? c.Application.Id : (int?)null
        })
        .ToListAsync();

    return Results.Ok(cards);
})
.WithName("ListUserCards");


// Deletes all Pending PassBuyCardApplications for the authenticated user.
app.MapMethods("/PassBuy/applications/stale", new[] { "DELETE", "POST" }, async (AppDbContext db, HttpContext context) =>
{
    Console.WriteLine("→ /PassBuy/applications/stale called");

    var authHeader = context.Request.Headers["Authorization"].ToString();
    if (!authHeader.StartsWith("Bearer ")) return Results.Unauthorized();

    var token = authHeader["Bearer ".Length..].Trim();
    var principal = ValidateJwt.ValidateJwtToken(token, cfg);

    var sub = principal.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
    if (!Guid.TryParse(sub, out var userId)) return Results.Unauthorized();

    // Pending app ids
    var pendingIds = await db.PassBuyCardApplications
        .Where(a => a.UserId == userId && a.status == "Pending")
        .Select(a => a.Id)
        .ToListAsync();

    // Linked to a card? (skip)
    var linkedIds = await db.PassBuyCards
        .Where(c => pendingIds.Contains(EF.Property<int>(c, "ApplicationId")))
        .Select(c => EF.Property<int>(c, "ApplicationId"))
        .Distinct()
        .ToListAsync();

    var deletableIds = pendingIds.Except(linkedIds).ToList();

    using var tx = await db.Database.BeginTransactionAsync();
    try
    {
        var edus = await db.EducationDetails.Where(e => deletableIds.Contains(e.ApplicationId)).ToListAsync();
        var trans = await db.TransportEmploymentDetails.Where(t => deletableIds.Contains(t.ApplicationId)).ToListAsync();
        db.EducationDetails.RemoveRange(edus);
        db.TransportEmploymentDetails.RemoveRange(trans);

        var apps = await db.PassBuyCardApplications.Where(a => deletableIds.Contains(a.Id)).ToListAsync();
        db.PassBuyCardApplications.RemoveRange(apps);

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        Console.WriteLine($"✓ cleanup ok: apps={apps.Count}, details={edus.Count + trans.Count}, skipped={linkedIds.Count}");
        return Results.Ok(new { deletedApplications = apps.Count, deletedDetails = edus.Count + trans.Count, skippedLinkedToCard = linkedIds.Count });
    }
    catch (Exception ex)
    {
        await tx.RollbackAsync();
        Console.WriteLine($"✗ cleanup error: {ex.Message}");
        return Results.Problem($"Failed to delete stale applications: {ex.Message}");
    }
});


// Delete PassBuy card
app.MapPost("/PassBuy/cards/{cardId:int}/delete", async (AppDbContext db, HttpContext context, int cardId) =>
{
    var authHeader = context.Request.Headers["Authorization"].ToString();
    if (!authHeader.StartsWith("Bearer ")) return Results.Unauthorized();

    var token = authHeader["Bearer ".Length..].Trim();
    var principal = ValidateJwt.ValidateJwtToken(token, cfg);

    var sub = principal.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
    if (!Guid.TryParse(sub, out var userId)) return Results.Unauthorized();

    var card = await db.PassBuyCards
        .Where(c => c.Id == cardId && c.UserId == userId)
        .FirstOrDefaultAsync();

    if (card is null) return Results.NotFound("Card not found");

    db.PassBuyCards.Remove(card);
    await db.SaveChangesAsync();

    return Results.Ok(new { deleted = true, cardId });
})
.WithName("DeletePassBuyCard");


// List education providers (id, name, eduCode)
app.MapGet("/PassBuy/educationProviders", async (AppDbContext db) =>
{
    var list = await db.EducationProviders
        .OrderBy(p => p.Name)
        .Select(p => new { id = p.Id, name = p.Name, eduCode = p.EduCode })
        .ToListAsync();
    return Results.Ok(list);
})
.WithName("ListEducationProviders");


// List transport employers (id, name)
app.MapGet("/PassBuy/transportEmployers", async (AppDbContext db) =>
{
    var list = await db.TransportEmployers
        .OrderBy(t => t.Name)
        .Select(t => new { id = t.Id, name = t.Name })
        .ToListAsync();
    return Results.Ok(list);
})
.WithName("ListTransportEmployers");


app.Run();
