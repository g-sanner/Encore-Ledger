using EncoreLedger.Data;
using Microsoft.EntityFrameworkCore;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Build per-user SQLite path
var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
var appFolder = Path.Combine(localAppData, "EncoreLedger");
// ensure folder exists
Directory.CreateDirectory(appFolder);
var dbPath = Path.Combine(appFolder, "EncoreLedger.db");

// Add MVC and SQLite services
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

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

// Auto-open browser
var url = "http://localhost:5000";
app.Urls.Add(url);

try
{
    var psi = new System.Diagnostics.ProcessStartInfo
    {
        FileName = url,
        UseShellExecute = true
    };
    System.Diagnostics.Process.Start(psi);
}
catch { }

// Run the app
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