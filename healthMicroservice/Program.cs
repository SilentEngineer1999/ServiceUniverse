using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;                 // <-- fixes JwtRegisteredClaimNames
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using HttpJsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions; // <-- disambiguates JsonOptions



const string JwtIssuer = "HealthService.AllInOne";
const string JwtAudience = "HealthClients";
const string JwtSecret = "change-this-very-long-secret-at-least-32-characters";
const int AccessTokenMinutes = 60;

// ===== App setup =====
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.Configure<HttpJsonOptions>(opt =>   // <-- use the aliased JsonOptions
{
    opt.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = JwtIssuer,
            ValidAudience = JwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret)),
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

// ===== In-memory stores =====
var users = new List<User>(); // auth
var refreshTokens = new Dictionary<string, string>(); // refresh -> userId

var doctors = new List<Doctor>
{
    new() { Id = "d1", Name = "Dr. Ayesha Rahman", Specialty = "General Practice",
        Slots = new() { "2025-10-16T09:00","2025-10-16T09:30","2025-10-16T10:00" } },

    new() { Id = "d2", Name = "Dr. Samuel Lee", Specialty = "Cardiology",
        Slots = new() { "2025-10-16T11:00","2025-10-16T11:30","2025-10-16T12:00" } },

    new() { Id = "d3", Name = "Dr. Priya Nair", Specialty = "Dermatology",
        Slots = new() { "2025-10-16T13:00","2025-10-16T13:30","2025-10-16T14:00" } },

    new() { Id = "d4", Name = "Dr. Miguel Santos", Specialty = "Pediatrics",
        Slots = new() { "2025-10-16T09:15","2025-10-16T09:45","2025-10-16T10:15" } },

    new() { Id = "d5", Name = "Dr. Olivia Chen", Specialty = "Orthopedics",
        Slots = new() { "2025-10-16T15:00","2025-10-16T15:30","2025-10-16T16:00" } }
};

var appointments = new List<Appointment>
{
    new() { Id = "a1", PatientName = "Maria H.", DoctorId = "d1", Time = "2025-10-16T09:00" }
};

// ===== Helpers =====
static void HashPassword(string password, out byte[] salt, out byte[] hash)
{
    salt = RandomNumberGenerator.GetBytes(16);
    using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
    hash = pbkdf2.GetBytes(32);
}
static bool VerifyPassword(string password, byte[] salt, byte[] hash)
{
    using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
    var calc = pbkdf2.GetBytes(32);
    return CryptographicOperations.FixedTimeEquals(calc, hash);
}
static string NewRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
string IssueAccessToken(User u)
{
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, u.Id),
        new Claim(JwtRegisteredClaimNames.Email, u.Email),
        new Claim(ClaimTypes.Name, u.Name),
        new Claim(ClaimTypes.Role, u.Role)
    };
    var token = new JwtSecurityToken(
        issuer: JwtIssuer, audience: JwtAudience, claims: claims,
        expires: DateTime.UtcNow.AddMinutes(AccessTokenMinutes), signingCredentials: creds);
    return new JwtSecurityTokenHandler().WriteToken(token);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// ===== Auth endpoints =====
app.MapGet("/api/auth/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/auth/signup", ([FromBody] SignupDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name) ||
        string.IsNullOrWhiteSpace(dto.Email) ||
        string.IsNullOrWhiteSpace(dto.Password))
        return Results.BadRequest("name, email, password required.");

    if (users.Any(u => u.Email.Equals(dto.Email.Trim(), StringComparison.OrdinalIgnoreCase)))
        return Results.Conflict("Email already registered.");

    HashPassword(dto.Password, out var salt, out var hash);
    var user = new User
    {
        Id = $"u{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
        Name = dto.Name.Trim(),
        Email = dto.Email.Trim(),
        PasswordSalt = Convert.ToBase64String(salt),
        PasswordHash = Convert.ToBase64String(hash),
        Role = string.IsNullOrWhiteSpace(dto.Role) ? "patient" : dto.Role.Trim().ToLower()
    };
    users.Add(user);

    var access = IssueAccessToken(user);
    var refresh = NewRefreshToken();
    refreshTokens[refresh] = user.Id;
    return Results.Ok(new { accessToken = access, refreshToken = refresh, user = new { user.Id, user.Name, user.Email, user.Role } });
});

app.MapPost("/api/auth/login", ([FromBody] LoginDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        return Results.BadRequest("email and password required.");

    var user = users.FirstOrDefault(u => u.Email.Equals(dto.Email.Trim(), StringComparison.OrdinalIgnoreCase));
    if (user is null) return Results.Unauthorized();

    var ok = VerifyPassword(dto.Password, Convert.FromBase64String(user.PasswordSalt), Convert.FromBase64String(user.PasswordHash));
    if (!ok) return Results.Unauthorized();

    var access = IssueAccessToken(user);
    var refresh = NewRefreshToken();
    refreshTokens[refresh] = user.Id;
    return Results.Ok(new { accessToken = access, refreshToken = refresh, user = new { user.Id, user.Name, user.Email, user.Role } });
});

app.MapPost("/api/auth/refresh", ([FromBody] RefreshDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.RefreshToken)) return Results.BadRequest("refreshToken required.");
    if (!refreshTokens.TryGetValue(dto.RefreshToken, out var userId)) return Results.Unauthorized();

    var user = users.FirstOrDefault(u => u.Id == userId);
    if (user is null) return Results.Unauthorized();

    refreshTokens.Remove(dto.RefreshToken);
    var newRefresh = NewRefreshToken();
    refreshTokens[newRefresh] = user.Id;

    var access = IssueAccessToken(user);
    return Results.Ok(new { accessToken = access, refreshToken = newRefresh });
});

app.MapGet("/api/auth/me", (ClaimsPrincipal p) =>
{
    if (!(p.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var sub = p.FindFirstValue(JwtRegisteredClaimNames.Sub);
    var email = p.FindFirstValue(ClaimTypes.Email) ?? p.FindFirstValue(JwtRegisteredClaimNames.Email);
    var name = p.FindFirstValue(ClaimTypes.Name) ?? "";
    var role = p.FindFirstValue(ClaimTypes.Role) ?? "patient";
    return Results.Ok(new { id = sub, name, email, role });
}).RequireAuthorization();

app.MapPost("/api/auth/logout", ([FromBody] RefreshDto dto) =>
{
    if (!string.IsNullOrWhiteSpace(dto.RefreshToken)) refreshTokens.Remove(dto.RefreshToken);
    return Results.Ok();
});

// ===== Data endpoints =====
app.MapGet("/api/doctors", () => Results.Ok(doctors));

app.MapGet("/api/appointments", () =>
{
    var result = appointments.Select(a => new AppointmentDto
    {
        Id = a.Id,
        PatientName = a.PatientName,
        DoctorId = a.DoctorId,
        DoctorName = doctors.FirstOrDefault(d => d.Id == a.DoctorId)?.Name,
        Time = a.Time
    });
    return Results.Ok(result);
});

app.MapPost("/api/appointments", ([FromBody] CreateAppointmentDto dto, ClaimsPrincipal userPrincipal) =>
{
    if (!userPrincipal.Identity?.IsAuthenticated ?? true)
        return Results.Unauthorized();

    if (string.IsNullOrWhiteSpace(dto.PatientName) ||
        string.IsNullOrWhiteSpace(dto.DoctorId) ||
        string.IsNullOrWhiteSpace(dto.Time))
        return Results.BadRequest("patientName, doctorId and time are required.");

    var doctor = doctors.FirstOrDefault(d => d.Id == dto.DoctorId);
    if (doctor is null) return Results.NotFound("Doctor not found.");
    if (!doctor.Slots.Contains(dto.Time)) return Results.BadRequest("Selected time is not in doctor's available slots.");

    var appt = new Appointment
    {
        Id = $"a{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
        PatientName = dto.PatientName.Trim(),
        DoctorId = dto.DoctorId,
        Time = dto.Time
    };
    appointments.Insert(0, appt);

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

// ===== Models / DTOs =====
record User
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string PasswordSalt { get; set; } = default!;
    public string Role { get; set; } = "patient";
}

record Doctor { public string Id { get; set; } = default!; public string Name { get; set; } = default!; public string Specialty { get; set; } = default!; public List<string> Slots { get; set; } = new(); }
record Appointment { public string Id { get; set; } = default!; public string PatientName { get; set; } = default!; public string DoctorId { get; set; } = default!; public string Time { get; set; } = default!; }
record CreateAppointmentDto { public string PatientName { get; set; } = default!; public string DoctorId { get; set; } = default!; public string Time { get; set; } = default!; }
record AppointmentDto { public string Id { get; set; } = default!; public string PatientName { get; set; } = default!; public string DoctorId { get; set; } = default!; public string? DoctorName { get; set; } public string Time { get; set; } = default!; }

record SignupDto(string Name, string Email, string Password, string? Role);
record LoginDto(string Email, string Password);
record RefreshDto(string RefreshToken);
