using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using HttpJsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

using HealthApi.Data;
using HealthApi.Models;
using HealthApi.AuthController;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

// Database
builder.Services.AddDbContext<HealthDbContext>(options =>
    options.UseNpgsql(cfg.GetConnectionString("DefaultConnection")));

// JWT + helpers
builder.Services.Configure<JwtOptions>(cfg.GetSection("Jwt"));
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();

// Pull from appsettings.json
var jwtKey      = cfg["Jwt:Key"]      ?? throw new InvalidOperationException("Missing Jwt:Key");
var jwtIssuer   = cfg["Jwt:Issuer"]   ?? throw new InvalidOperationException("Missing Jwt:Issuer");
var jwtAudience = cfg["Jwt:Audience"] ?? throw new InvalidOperationException("Missing Jwt:Audience");

// CORS / JSON
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.Configure<HttpJsonOptions>(opt =>
{
    opt.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Authentication + Authorization
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

// Add openApi
builder.Services.AddOpenApi();

var app = builder.Build();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Seed database on startup
await SeedDb.EnsureCreatedAndSeedAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// ===== Auth endpoints =====
app.MapGet("/api/auth/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/auth/signup", async (
    [FromBody] SignupDto dto,
    HealthDbContext db,
    IPasswordHasher hasher,
    IJwtTokenService jwt,
    IRefreshTokenGenerator refreshGen) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name) ||
        string.IsNullOrWhiteSpace(dto.Email) ||
        string.IsNullOrWhiteSpace(dto.Password))
        return Results.BadRequest("name, email, password required.");

    var emailNorm = dto.Email.Trim().ToLower();
    var exists = await db.Users.AnyAsync(u => u.Email == emailNorm);
    if (exists) return Results.Conflict("Email already registered.");

    hasher.HashPassword(dto.Password, out var salt, out var hash);
    var user = new User
    {
        Id = $"u{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
        Name = dto.Name.Trim(),
        Email = emailNorm,
        PasswordSalt = Convert.ToBase64String(salt),
        PasswordHash = Convert.ToBase64String(hash),
        Role = string.IsNullOrWhiteSpace(dto.Role) ? "patient" : dto.Role.Trim().ToLower()
    };
    db.Users.Add(user);
    await db.SaveChangesAsync();

    var access = jwt.IssueAccessToken(user);
    var refresh = refreshGen.NewRefreshToken();
    db.RefreshTokens.Add(new RefreshToken { Token = refresh, UserId = user.Id, CreatedUtc = DateTime.UtcNow });
    await db.SaveChangesAsync();

    return Results.Ok(new { accessToken = access, refreshToken = refresh, user = new { user.Id, user.Name, user.Email, user.Role } });
});

app.MapPost("/api/auth/login", async (
    [FromBody] LoginDto dto,
    HealthDbContext db,
    IPasswordHasher hasher,
    IJwtTokenService jwt,
    IRefreshTokenGenerator refreshGen) =>
{
    if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        return Results.BadRequest("email and password required.");

    var emailNorm = dto.Email.Trim().ToLower();
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == emailNorm);
    if (user is null) return Results.Unauthorized();

    var ok = hasher.VerifyPassword(dto.Password,
        Convert.FromBase64String(user.PasswordSalt),
        Convert.FromBase64String(user.PasswordHash));
    if (!ok) return Results.Unauthorized();

    var access = jwt.IssueAccessToken(user);
    var refresh = refreshGen.NewRefreshToken();
    db.RefreshTokens.Add(new RefreshToken { Token = refresh, UserId = user.Id, CreatedUtc = DateTime.UtcNow });
    await db.SaveChangesAsync();

    return Results.Ok(new { accessToken = access, refreshToken = refresh, user = new { user.Id, user.Name, user.Email, user.Role } });
});

app.MapPost("/api/auth/refresh", async (
    [FromBody] RefreshDto dto,
    HealthDbContext db,
    IJwtTokenService jwt,
    IRefreshTokenGenerator refreshGen) =>
{
    if (string.IsNullOrWhiteSpace(dto.RefreshToken)) return Results.BadRequest("refreshToken required.");

    var rt = await db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == dto.RefreshToken);
    if (rt is null) return Results.Unauthorized();

    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == rt.UserId);
    if (user is null) return Results.Unauthorized();

    db.RefreshTokens.Remove(rt); // rotate
    var newRefresh = refreshGen.NewRefreshToken();
    db.RefreshTokens.Add(new RefreshToken { Token = newRefresh, UserId = user.Id, CreatedUtc = DateTime.UtcNow });
    await db.SaveChangesAsync();

    var access = jwt.IssueAccessToken(user);
    return Results.Ok(new { accessToken = access, refreshToken = newRefresh });
});

app.MapGet("/api/auth/me", async (ClaimsPrincipal p, HealthDbContext db) =>
{
    if (!(p.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var sub = p.FindFirstValue(JwtRegisteredClaimNames.Sub);
    if (string.IsNullOrEmpty(sub)) return Results.Unauthorized();

    var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == sub);
    if (user is null) return Results.Unauthorized();

    var email = user.Email;
    var name = user.Name ?? "";
    var role = user.Role ?? "patient";
    return Results.Ok(new { id = sub, name, email, role });
}).RequireAuthorization();

app.MapPost("/api/auth/logout", async ([FromBody] RefreshDto dto, HealthDbContext db) =>
{
    if (!string.IsNullOrWhiteSpace(dto.RefreshToken))
    {
        var rt = await db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == dto.RefreshToken);
        if (rt is not null) db.RefreshTokens.Remove(rt);
        await db.SaveChangesAsync();
    }
    return Results.Ok();
});

// ===== Data endpoints =====
app.MapGet("/api/doctors", async (HealthDbContext db) =>
{
    var docs = await db.Doctors.AsNoTracking().ToListAsync();
    return Results.Ok(docs);
});

app.MapGet("/api/appointments", async (HealthDbContext db) =>
{
    var result = await db.Appointments
        .AsNoTracking()
        .Select(a => new AppointmentDto
        {
            Id = a.Id,
            PatientName = a.PatientName,
            DoctorId = a.DoctorId,
            DoctorName = db.Doctors.Where(d => d.Id == a.DoctorId).Select(d => d.Name).FirstOrDefault(),
            Time = a.Time
        })
        .ToListAsync();
    return Results.Ok(result);
});

app.MapPost("/api/appointments", async ([FromBody] CreateAppointmentDto dto, ClaimsPrincipal userPrincipal, HealthDbContext db) =>
{
    if (!userPrincipal.Identity?.IsAuthenticated ?? true)
        return Results.Unauthorized();

    if (string.IsNullOrWhiteSpace(dto.PatientName) ||
        string.IsNullOrWhiteSpace(dto.DoctorId) ||
        string.IsNullOrWhiteSpace(dto.Time))
        return Results.BadRequest("patientName, doctorId and time are required.");

    var doctor = await db.Doctors.FirstOrDefaultAsync(d => d.Id == dto.DoctorId);
    if (doctor is null) return Results.NotFound("Doctor not found.");
    if (!(doctor.Slots?.Contains(dto.Time) ?? false)) return Results.BadRequest("Selected time is not in doctor's available slots.");

    var appt = new Appointment
    {
        Id = $"a{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
        PatientName = dto.PatientName.Trim(),
        DoctorId = dto.DoctorId,
        Time = dto.Time
    };
    db.Appointments.Add(appt);
    await db.SaveChangesAsync();

    var dtoOut = new AppointmentDto
    {
        Id = appt.Id,
        PatientName = appt.PatientName,
        DoctorId = appt.DoctorId,
        DoctorName = doctor.Name,
        Time = appt.Time
    };
    return Results.Created($"/api/appointments/{appt.Id}", dtoOut);
}).RequireAuthorization();

app.Run();
