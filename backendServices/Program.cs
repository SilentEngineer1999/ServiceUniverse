using backendServices.AuthController;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
var connectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=root;";

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

using (var conn = new NpgsqlConnection(connectionString))
{
    conn.Open();
    var createTableCmd = new NpgsqlCommand(@"
        CREATE TABLE IF NOT EXISTS users (
            id SERIAL PRIMARY KEY,
            firstName TEXT NOT NULL,
            lastName TEXT NOT NULL,
            age INT NOT NULL,
            email TEXT NOT NULL UNIQUE,
            password TEXT NOT NULL,
            salt BYTEA NOT NULL
        )", conn);

    createTableCmd.ExecuteNonQuery();
}

PasswordManager passwordManager = new();
AuthGenerator authGenerator = new();

// POST endpoint to insert a user
app.MapPost("/signUp", async (string fname, string lname, int age, string email, string password) =>
{
    try
    {
        // Hash the password and get salt
        var (hashPassword, salt) = passwordManager.HashPassword(password);

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "INSERT INTO users (firstName, lastName, age, password, salt, email) VALUES (@firstName, @lastName, @age, @password, @salt, @email) RETURNING id", conn);

        cmd.Parameters.AddWithValue("firstName", fname);
        cmd.Parameters.AddWithValue("lastName", lname);
        cmd.Parameters.AddWithValue("age", age);
        cmd.Parameters.AddWithValue("password", hashPassword);
        cmd.Parameters.AddWithValue("salt", salt); // store as byte[]
        cmd.Parameters.AddWithValue("email", email);

        var result = await cmd.ExecuteScalarAsync();
        if (result is null)
        {
            return Results.Problem("Failed to create user.");
        }

        int userId = Convert.ToInt32(result);
        var token = authGenerator.GenerateJwtToken(email, userId);

        return Results.Ok(new { message = "User inserted successfully", token });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// POST sign in
app.MapPost("/signIn", async (string email, string password) =>
{
    try
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand("SELECT id, firstName, lastName, age, email, password, salt FROM users WHERE email = @Email", conn);
        cmd.Parameters.AddWithValue("Email", email);
        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            int userId = reader.GetInt32(reader.GetOrdinal("id"));
            string storedPassword = reader.GetString(reader.GetOrdinal("password"));
            byte[] storedSalt = (byte[])reader["salt"]; // read as byte[]

            bool passwordVerify = passwordManager.VerifyPassword(password, storedPassword, storedSalt);
            if (passwordVerify)
            {
                var token = authGenerator.GenerateJwtToken(email, userId);
                return Results.Ok(new { message = "User Authenticated", token });
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

app.MapGet("/protected", (HttpContext context) =>
{
    string? authHeader = context.Request.Headers["Authorization"];
    if (authHeader == null || !authHeader.StartsWith("Bearer "))
        return Results.Unauthorized();

    var token = authHeader.Substring("Bearer ".Length).Trim();

    try
    {
        var principal = ValidateJwt.ValidateJwtToken(token);
        var userId = principal.FindFirst("userId")?.Value;

        return Results.Ok(new { message = "Valid token", userId });
    }
    catch
    {
        return Results.Unauthorized();
    }
});

app.Run();
