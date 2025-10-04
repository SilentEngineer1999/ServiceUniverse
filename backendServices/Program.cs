using backendServices.AuthController;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Default");

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
            password TEXT NOT NULL,
            email TEXT NOT NULL UNIQUE
        )", conn);
    createTableCmd.ExecuteNonQuery();
}

PasswordManager passwordManager = new();

// POST endpoint to insert a user
app.MapPost("/signUp", async (string fname, string lname, int age, string email, string password) =>
{
    try
    {
        string hashPassword = passwordManager.HashPassword(password);
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "INSERT INTO users (firstName, lastName, age, email, password) VALUES (@firstName, @lastName, @age, @email, @password)", conn);

        cmd.Parameters.AddWithValue("firstName", fname);
        cmd.Parameters.AddWithValue("lastName", lname);
        cmd.Parameters.AddWithValue("age", age);
        cmd.Parameters.AddWithValue("email", email);
        cmd.Parameters.AddWithValue("password", hashPassword);

        await cmd.ExecuteNonQueryAsync();

        return Results.Ok(new { message = "User inserted successfully" });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});


app.MapGet("/signIn", async (string email, string password) =>
{
    try
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand("SELECT id, firstName, lastName, age, email, password FROM users WHERE email = @Email", conn);
        cmd.Parameters.AddWithValue("Email", email);
        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var user = new
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                FirstName = reader.GetString(reader.GetOrdinal("firstName")),
                LastName = reader.GetString(reader.GetOrdinal("lastName")),
                Age = reader.GetInt32(reader.GetOrdinal("age")),
                Email = reader.GetString(reader.GetOrdinal("email")),
                Password = reader.GetString(reader.GetOrdinal("password"))
            };

            bool passwordVerify = passwordManager.VerifyPassword(password, user.Password);
            if (passwordVerify)
            {
                return Results.Ok("User Authenticated");
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

app.Run();
