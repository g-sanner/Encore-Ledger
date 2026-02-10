using Microsoft.Data.Sqlite;

var connectionString = "Data Source=EncoreLedger.db";

using var connection = new SqliteConnection(connectionString);
connection.Open();

var command = connection.CreateCommand();

// Initialize schema
var sql = File.ReadAllText("Sql/init.sql");
command.CommandText = sql;
command.ExecuteNonQuery();

// SQLite test
/* 
command.CommandText = "SELECT name FROM sqlite_master WHERE type='table';";

using var reader = command.ExecuteReader();
Console.WriteLine("Tables in database:");
while (reader.Read())
{
    Console.WriteLine($"- {reader.GetString(0)}");
}
*/

var builder = WebApplication.CreateBuilder(args);

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

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
