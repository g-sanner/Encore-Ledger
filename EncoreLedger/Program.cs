using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

var connectionString = "Data Source=EncoreLedger.db";

// Add MVC service
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Initialize schema
using (var connection = new SqliteConnection(connectionString))
{
    connection.Open();

    var command = connection.CreateCommand();
    var sql = File.ReadAllText("Sql/init.sql");
    command.CommandText = sql;
    command.ExecuteNonQuery();
}

// Configure middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Enable MVC routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

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