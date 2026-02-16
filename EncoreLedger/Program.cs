using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

var connectionString = "Data Source=EncoreLedger.db";

// Initialize schema
using (var connection = new SqliteConnection(connectionString))
{
    connection.Open();

    var command = connection.CreateCommand();
    var sql = File.ReadAllText("Sql/init.sql");
    command.CommandText = sql;
    command.ExecuteNonQuery();
}

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

/*
app.MapGet("/transactions", () =>
{
    using var connection = new SqliteConnection(connectionString);
    connection.Open();

    var command = connection.CreateCommand();
    command.CommandText = """
        SELECT IDTransaction, Description, Amount FROM "Transaction"
    """;

    using var reader = command.ExecuteReader();

    var results = new List<object>();

    while (reader.Read())
    {
        results.Add(new
        {
            IDTransaction = reader.GetInt32(0),
            Description = reader.GetString(1),
            Amount = reader.GetDecimal(2)
        });
    }

    return results;
});
*/

app.Run();
