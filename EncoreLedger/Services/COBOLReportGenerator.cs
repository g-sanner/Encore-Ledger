using EncoreLedger.Data;
using EncoreLedger.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;

namespace EncoreLedger.Services
{
    // COBOL implementation of report generation.
    // Serializes transaction data to a file, calls a compiled COBOL executable,
    // and parses the output back into ReportData.
    public class COBOLReportGenerator : IReportGenerator
    {
        private readonly ApplicationDbContext _context;
        private readonly string _cobolExecutablePath;
        private readonly string _tempDirectory;
        private readonly ILogger<COBOLReportGenerator> _logger;

        public COBOLReportGenerator(ApplicationDbContext context, IConfiguration configuration, ILogger<COBOLReportGenerator> logger)
        {
            _context = context;
            _logger = logger;

            // Get COBOL executable path from configuration
            var configuredPath = configuration["ReportGeneration:CobolExecutablePath"]
                ?? throw new InvalidOperationException("ReportGeneration:CobolExecutablePath not configured");
            _cobolExecutablePath = ResolveExecutablePath(configuredPath);

            // Use app data folder for temp files
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _tempDirectory = Path.Combine(appDataPath, "EncoreLedger", "Reports", "Temp");
            Directory.CreateDirectory(_tempDirectory);
        }

        // Generates report data by calling COBOL executable.
        // COBOL interface contract:
        // Input: CSV file with columns: TransactionDate, Description, Amount, CategoryName
        // Output: JSON file with ReportData structure
        public async Task<ReportData> GenerateReportData(DateTime startDate, DateTime endDate)
        {
            try
            {
                // Fetch transactions and prepare input file
                var transactions = await _context.Transactions
                    .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
                    .Include(t => t.Category)
                    .ToListAsync();

                string inputFilePath = await WriteInputFile(startDate, endDate, transactions);
                string outputFilePath = Path.Combine(_tempDirectory, $"report_output_{DateTime.Now.Ticks}.json");

                // Call COBOL executable
                await ExecuteCobolProgram(inputFilePath, outputFilePath);

                // Parse and return output
                var reportData = await ReadOutputFile(outputFilePath, startDate, endDate);

                // Cleanup temp files
                try
                {
                    if (File.Exists(inputFilePath)) File.Delete(inputFilePath);
                    if (File.Exists(outputFilePath)) File.Delete(outputFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to cleanup temp files: {ex.Message}");
                }

                return reportData;
            }
            catch (Exception ex)
            {
                _logger.LogError($"COBOL report generation failed: {ex.Message}");
                throw;
            }
        }

        // Renders report data as plain text.
        // Delegates to a helper method (same as C# implementation).
        public string RenderReportAsText(ReportData reportData)
        {
            return RenderReportAsTextInternal(reportData);
        }

        // PDF rendering not yet implemented for COBOL.
        public async Task<byte[]> RenderReportAsPDF(ReportData reportData)
        {
            throw new NotImplementedException("PDF rendering not yet implemented for COBOL. Use RenderReportAsText and convert externally.");
        }

        // ===================== Private Helper Methods =====================

        private async Task<string> WriteInputFile(DateTime startDate, DateTime endDate, List<EncoreLedger.Models.Transaction> transactions)
        {
            string filePath = Path.Combine(_tempDirectory, $"report_input_{DateTime.Now.Ticks}.csv");

            // Write CSV: TransactionDate,Description,Amount,CategoryName
            using (var writer = new StreamWriter(filePath))
            {
                await writer.WriteLineAsync("TransactionDate,Description,Amount,CategoryName");

                foreach (var tx in transactions)
                {
                    string description = (tx.Description ?? "").Replace(",", ";"); // Escape commas
                    string categoryName = tx.Category?.Name ?? "Uncategorized";
                    await writer.WriteLineAsync($"{tx.TransactionDate:yyyy-MM-dd},{description},{tx.Amount},{categoryName}");
                }
            }

            _logger.LogInformation($"COBOL input file created: {filePath}");
            return filePath;
        }

        private async Task ExecuteCobolProgram(string inputFilePath, string outputFilePath)
        {
            if (!File.Exists(_cobolExecutablePath))
            {
                throw new FileNotFoundException($"COBOL executable not found: {_cobolExecutablePath}");
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = _cobolExecutablePath,
                Arguments = $"\"{inputFilePath}\" \"{outputFilePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processInfo))
            {
                if (process == null)
                {
                    throw new InvalidOperationException("Failed to start COBOL process");
                }

                // Wait for process with timeout (60 seconds)
                bool completed = process.WaitForExit(60000);
                if (!completed)
                {
                    process.Kill();
                    throw new TimeoutException("COBOL program execution timed out after 60 seconds");
                }

                if (process.ExitCode != 0)
                {
                    string error = await process.StandardError.ReadToEndAsync();
                    throw new InvalidOperationException($"COBOL program failed with exit code {process.ExitCode}: {error}");
                }

                _logger.LogInformation($"COBOL program executed successfully. Output: {outputFilePath}");
            }
        }

        private async Task<ReportData> ReadOutputFile(string outputFilePath, DateTime startDate, DateTime endDate)
        {
            if (!File.Exists(outputFilePath))
            {
                throw new FileNotFoundException($"COBOL output file not found: {outputFilePath}");
            }

            string jsonContent = await File.ReadAllTextAsync(outputFilePath);

            try
            {
                var reportData = JsonSerializer.Deserialize<ReportData>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (reportData == null)
                {
                    throw new InvalidOperationException("Failed to deserialize COBOL output");
                }

                return reportData;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse COBOL JSON output: {ex.Message}", ex);
            }
        }

        // Internal text rendering (same logic as C# implementation).
        // Could be shared in the future if extracted to a common utility class.
        private string RenderReportAsTextInternal(ReportData reportData)
        {
            var lines = new List<string>();

            // Header
            lines.Add(new string('=', 59));
            lines.Add(new string(' ', 15) + "ENCORE LEDGER");
            lines.Add(new string('=', 59));
            lines.Add(new string('=', 59));

            // Determine report type and format date accordingly
            bool isSameDay = reportData.PeriodStartDate.Date == reportData.PeriodEndDate.Date;
            if (isSameDay)
            {
                lines.Add(new string(' ', 16) + "END-OF-DAY FINANCIAL REPORT");
                lines.Add(new string('=', 59));
                lines.Add($"Date: {reportData.PeriodStartDate:MMMM dd, yyyy}");
            }
            else
            {
                lines.Add(new string(' ', 14) + "END-OF-MONTH FINANCIAL REPORT");
                lines.Add(new string('=', 59));
                lines.Add($"Month: {reportData.PeriodStartDate:MMMM yyyy}");
            }

            lines.Add("_".PadRight(59, '_'));
            lines.Add("");

            // Summary section
            lines.Add("SUMMARY");
            lines.Add("_".PadRight(59, '_'));
            lines.Add($"Total Income      : {reportData.Summary?.TotalIncome:$ 0.00}");
            lines.Add($"Total Expenses    : {reportData.Summary?.TotalExpenses:$ 0.00}");
            lines.Add("_".PadRight(59, '_'));
            lines.Add($"NET BALANCE       : {reportData.Summary?.NetBalance:$ 0.00}");
            lines.Add("_".PadRight(59, '_'));
            lines.Add("");

            // Category Breakdown section
            lines.Add("CATEGORY BREAKDOWN");
            lines.Add("_".PadRight(59, '_'));
            lines.Add("");

            // Table header
            lines.Add($"{"Category",-20} {"Income",15} {"Expense",15} {"Net",15}");
            lines.Add("_".PadRight(59, '_'));

            // Table rows
            foreach (var category in reportData.CategoryBreakdowns)
            {
                lines.Add($"{category.CategoryName!,-20} {category.Income,15:$ 0.00} {category.Expense,15:$ 0.00} {category.Net,15:$ 0.00}");
            }

            lines.Add("_".PadRight(59, '_'));
            lines.Add("");

            // Highest expense category
            var highestExpenseCategory = reportData.CategoryBreakdowns
                .OrderByDescending(c => c.Expense)
                .FirstOrDefault();

            if (highestExpenseCategory != null && highestExpenseCategory.Expense > 0)
            {
                lines.Add($"Highest Expense Category: {highestExpenseCategory.CategoryName} ({highestExpenseCategory.Expense:$ 0.00})");
            }

            lines.Add("");
            lines.Add($"Report Generated: {DateTime.Now:dd-MMM-yyyy}");

            return string.Join("\n", lines);
        }

        public string GetImplementationName()
        {
            return "COBOL";
        }

        private static string ResolveExecutablePath(string configuredPath)
        {
            if (Path.IsPathRooted(configuredPath))
            {
                return configuredPath;
            }

            var candidates = new[]
            {
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath)),
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), configuredPath))
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            // Return a deterministic candidate for diagnostics if the file does not yet exist.
            return candidates[0];
        }
    }
}
