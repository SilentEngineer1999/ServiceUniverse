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
            "INSERT INTO users (firstName, lastName, age, password, salt, email) VALUES (@firstName, @lastName, @age, @password, @salt, @email)", conn);

        cmd.Parameters.AddWithValue("firstName", fname);
        cmd.Parameters.AddWithValue("lastName", lname);
        cmd.Parameters.AddWithValue("age", age);
        cmd.Parameters.AddWithValue("password", hashPassword);
        cmd.Parameters.AddWithValue("salt", salt); // store as byte[]
        cmd.Parameters.AddWithValue("email", email);

        await cmd.ExecuteNonQueryAsync();

        return Results.Ok(new { message = "User inserted successfully" });
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
            string storedPassword = reader.GetString(reader.GetOrdinal("password"));
            byte[] storedSalt = (byte[])reader["salt"]; // read as byte[]

            bool passwordVerify = passwordManager.VerifyPassword(password, storedPassword, storedSalt);
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

// fetch utility bill(s)
app.MapGet("/fetchUtilityBill", async (int user_id) =>
{
    try
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(@"
            SELECT 
                utility_id,
                user_id,
                gas_usage,
                gas_rate,
                water_usage,
                water_rate,
                electricity_usage,
                electricity_rate,
                (gas_usage * gas_rate + water_usage * water_rate + electricity_usage * electricity_rate) AS total_bill,
                due_date,
                penalty,
                status
            FROM utility
            WHERE user_id = @Id
        ", conn);

        cmd.Parameters.AddWithValue("Id", user_id);

        await using var reader = await cmd.ExecuteReaderAsync();
        var bills = new List<object>();

        while (await reader.ReadAsync()) // loop through all rows
        {
            bills.Add(new
            {
                UtilityId = reader.GetInt32(reader.GetOrdinal("utility_id")),
                UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                GasUsage = reader.GetInt32(reader.GetOrdinal("gas_usage")),
                GasRate = reader.GetInt32(reader.GetOrdinal("gas_rate")),
                WaterUsage = reader.GetInt32(reader.GetOrdinal("water_usage")),
                WaterRate = reader.GetInt32(reader.GetOrdinal("water_rate")),
                ElectricityUsage = reader.GetInt32(reader.GetOrdinal("electricity_usage")),
                ElectricityRate = reader.GetInt32(reader.GetOrdinal("electricity_rate")),
                TotalBill = reader.GetInt32(reader.GetOrdinal("total_bill")),
                DueDate = reader.GetDateTime(reader.GetOrdinal("due_date")),
                Penalty = reader.GetInt32(reader.GetOrdinal("penalty")),
                Status = reader.GetString(reader.GetOrdinal("status"))
            });
        }

        if (bills.Count > 0)
        {
            return Results.Ok(bills);
        }
        else
        {
            return Results.NotFound(new { message = "No utility records found for this user" });
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// confirm payment
app.MapGet("/paymentConfirmed", async (int user_id, int utility_id) =>
{
    try
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(@"
            UPDATE utility
            SET status = 'paid'
            WHERE user_id = @UserId AND utility_id = @UtilityId
        ", conn);

        cmd.Parameters.AddWithValue("UserId", user_id);
        cmd.Parameters.AddWithValue("UtilityId", utility_id);

        int rowsAffected = await cmd.ExecuteNonQueryAsync();

        if (rowsAffected > 0)
        {
            return Results.Ok(new { message = "Payment confirmed, status updated to paid" });
        }
        else
        {
            return Results.NotFound(new { message = "No matching utility record found" });
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});


app.Run();
