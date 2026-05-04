using EncoreLedger.Data;
using EncoreLedger.Models.ViewModels;

namespace EncoreLedger.Services
{
    // Attempts COBOL report generation when enabled and the executable is present;
    // on any failure, uses C# report generation.
    public class FallbackReportGenerator : IReportGenerator
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly CSharpReportGenerator _csharp;
        private readonly ILogger<FallbackReportGenerator> _logger;
        private readonly ILogger<COBOLReportGenerator> _cobolLogger;
        public string LastExecutionEngine { get; private set; } = "Unknown";
        public string? LastFallbackReason { get; private set; }

        public FallbackReportGenerator(
            ApplicationDbContext context,
            IConfiguration configuration,
            CSharpReportGenerator csharp,
            ILogger<FallbackReportGenerator> logger,
            ILogger<COBOLReportGenerator> cobolLogger)
        {
            _context = context;
            _configuration = configuration;
            _csharp = csharp;
            _logger = logger;
            _cobolLogger = cobolLogger;
        }

        public async Task<ReportData> GenerateReportData(DateTime startDate, DateTime endDate)
        {
            LastFallbackReason = null;
            if (ShouldAttemptCobol(out var cobolPath))
            {
                try
                {
                    _logger.LogInformation("Generating report via COBOL executable: {Path}", cobolPath);
                    var cobol = new COBOLReportGenerator(_context, _configuration, _cobolLogger);
                    var result = await cobol.GenerateReportData(startDate, endDate);
                    LastExecutionEngine = "COBOL";
                    _logger.LogInformation("Report generation completed via COBOL.");
                    return result;
                }
                catch (Exception ex)
                {
                    LastFallbackReason = ex.Message;
                    _logger.LogWarning(ex, "COBOL report generation failed; falling back to C#.");
                }
            }
            else if (_configuration.GetValue<bool>("ReportGeneration:UseCobolForReporting"))
            {
                LastFallbackReason = "COBOL executable not found at configured path.";
                _logger.LogInformation(
                    "COBOL reporting is enabled but no executable was found at the configured path; using C#.");
            }

            LastExecutionEngine = "C# (fallback)";
            var fallbackResult = await _csharp.GenerateReportData(startDate, endDate);
            _logger.LogInformation("Report generation completed via C# fallback.");
            return fallbackResult;
        }

        public string RenderReportAsText(ReportData reportData) => _csharp.RenderReportAsText(reportData);

        public Task<byte[]> RenderReportAsPDF(ReportData reportData) => _csharp.RenderReportAsPDF(reportData);

        public string GetImplementationName() => "COBOL (C# fallback)";

        private bool ShouldAttemptCobol(out string path)
        {
            path = _configuration.GetValue<string>("ReportGeneration:CobolExecutablePath") ?? "";
            if (!_configuration.GetValue<bool>("ReportGeneration:UseCobolForReporting"))
                return false;
            if (string.IsNullOrWhiteSpace(path))
                return false;
            return true;
        }
    }
}
