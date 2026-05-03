using EncoreLedger.Data;
using EncoreLedger.Models.ViewModels;
using Microsoft.EntityFrameworkCore; 

namespace EncoreLedger.Services
{
    public class ReportGenerationService
    {
        private readonly ApplicationDbContext _context;

        public ReportGenerationService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Generates report data for a given date range by aggregating transactions by category.
        // Positive amounts = Income, Negative amounts = Expenses
        public async Task<ReportData> GenerateReportData(DateTime startDate, DateTime endDate)
        {
            // Fetch all transactions within the date range
            var transactions = await _context.Transactions
                .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
                .Include(t => t.Category)
                .ToListAsync();

            // Calculate summary
            decimal totalIncome = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
            decimal totalExpenses = Math.Abs(transactions.Where(t => t.Amount < 0).Sum(t => t.Amount));
            decimal netBalance = totalIncome - totalExpenses;

            // Group by category and calculate breakdown
            var categoryBreakdowns = transactions
                .GroupBy(t => t.Category?.Name ?? "Uncategorized")
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    decimal categoryIncome = g.Where(t => t.Amount > 0).Sum(t => t.Amount);
                    decimal categoryExpense = Math.Abs(g.Where(t => t.Amount < 0).Sum(t => t.Amount));
                    decimal categoryNet = categoryIncome - categoryExpense;

                    return new CategoryBreakdown
                    {
                        CategoryName = g.Key,
                        Income = categoryIncome,
                        Expense = categoryExpense,
                        Net = categoryNet
                    };
                })
                .ToList();

            return new ReportData
            {
                PeriodStartDate = startDate,
                PeriodEndDate = endDate,
                Summary = new ReportSummary
                {
                    TotalIncome = totalIncome,
                    TotalExpenses = totalExpenses,
                    NetBalance = netBalance
                },
                CategoryBreakdowns = categoryBreakdowns
            };
        }


        // Renders report data as formatted plain text (matching the PDF format)
        public string RenderReportAsText(ReportData reportData)
        {
            var lines = new List<string>();

            // Summary section
            lines.Add("SUMMARY");
            lines.Add($"Total Income      : {reportData.Summary?.TotalIncome:$ 0.00}");
            lines.Add($"Total Expenses    : {reportData.Summary?.TotalExpenses:$ 0.00}");
            lines.Add($"NET BALANCE       : {reportData.Summary?.NetBalance:$ 0.00}");

            // Category Breakdown section
            lines.Add("CATEGORY BREAKDOWN");

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

        // Renders report data as a PDF byte array (placeholder for implementation)
        // Will be implemented using iText7 or PdfSharp
        public async Task<byte[]> RenderReportAsPDF(ReportData reportData)
        {
            // TODO: Implement PDF rendering using iText7 or PdfSharp
            // For now, return empty byte array as placeholder
            throw new NotImplementedException("PDF rendering not yet implemented. Add iText7 or PdfSharp NuGet package.");
        }
    }
}
