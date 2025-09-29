using Microsoft.EntityFrameworkCore;
using BookingService.Data;
using BookingService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Typed HttpClient -> Base URL for PatientService
var baseUrl = builder.Configuration["PatientService:BaseUrl"]
              ?? Environment.GetEnvironmentVariable("PATIENT_SERVICE_URL")
              ?? "http://localhost:5001";
builder.Services.AddHttpClient<PatientClient>(client => client.BaseAddress = new Uri(baseUrl));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();