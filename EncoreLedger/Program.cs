using EncoreLedger.Data;
using EncoreLedger.Services;
using Microsoft.EntityFrameworkCore;

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

// Add business logic services
builder.Services.AddScoped<CSharpReportGenerator>();
builder.Services.AddScoped<ReportService>();

// Register IReportGenerator based on configuration with graceful fallback
var useCobol = builder.Configuration.GetValue<bool>("ReportGeneration:UseCobolForReporting");
var cobolExecutablePath = builder.Configuration.GetValue<string>("ReportGeneration:CobolExecutablePath");

builder.Services.AddScoped<IReportGenerator>(sp =>
{
    if (useCobol && !string.IsNullOrEmpty(cobolExecutablePath))
    {
        try
        {
            // Check if COBOL executable exists before creating the service
            if (File.Exists(cobolExecutablePath))
            {
                var logger = sp.GetRequiredService<ILogger<COBOLReportGenerator>>();
                var context = sp.GetRequiredService<ApplicationDbContext>();
                logger.LogInformation("Using COBOL Report Generator");
                return new COBOLReportGenerator(context, builder.Configuration, logger);
            }
            else
            {
                var logger = sp.GetRequiredService<ILogger<CSharpReportGenerator>>();
                logger.LogWarning($"COBOL executable not found at '{cobolExecutablePath}'. Falling back to C# implementation.");
            }
        }
        catch (Exception ex)
        {
            var logger = sp.GetRequiredService<ILogger<CSharpReportGenerator>>();
            logger.LogWarning($"Failed to initialize COBOL Report Generator: {ex.Message}. Falling back to C# implementation.");
        }
    }

    // Default to C#
    var csLogger = sp.GetRequiredService<ILogger<CSharpReportGenerator>>();
    csLogger.LogInformation("Using C# Report Generator");
    return new CSharpReportGenerator(sp.GetRequiredService<ApplicationDbContext>());
});

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