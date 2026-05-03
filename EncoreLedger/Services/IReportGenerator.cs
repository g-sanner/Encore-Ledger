using EncoreLedger.Models.ViewModels;

namespace EncoreLedger.Services
{
    // Interface for report generation implementations.
    // Allows swapping between C# and COBOL implementations.
    public interface IReportGenerator
    {
        // Generates report data for a given date range.
        // Returns structured data with Summary and CategoryBreakdowns.
        Task<ReportData> GenerateReportData(DateTime startDate, DateTime endDate);

        // Renders report data as plain text (for viewing in browser).
        string RenderReportAsText(ReportData reportData);

        // Renders report data as PDF bytes (placeholder or implementation).
        Task<byte[]> RenderReportAsPDF(ReportData reportData);

        // Returns the name of this implementation (e.g., "C#", "COBOL").
        // Used for UI display and diagnostics.
        string GetImplementationName();
    }
}
