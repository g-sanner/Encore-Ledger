using EncoreLedger.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add MVC and SQLite services
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=EncoreLedger.db"));

var app = builder.Build();

// Automatically apply migrations (creates database if missing)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
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