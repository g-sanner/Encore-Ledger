using Microsoft.AspNetCore.Mvc;
using EncoreLedger.Data;
using EncoreLedger.Services;
using Microsoft.EntityFrameworkCore; 
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EncoreLedger.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ReportGenerationService _reportGenerationService;
        private readonly ReportService _reportService;

        public ReportsController(ApplicationDbContext context, ReportGenerationService reportGenerationService, ReportService reportService)
        {
            _context = context;
            _reportGenerationService = reportGenerationService;
            _reportService = reportService;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Accounts = new SelectList(_context.Accounts, "IDAccount", "AccountName");

            var reports = await _context.Reports.OrderByDescending(r => r.DateCreated).ToListAsync();
            return View(reports);
        }

        [HttpPost]
        public async Task<IActionResult> GenerateReport(DateTime startDate, DateTime? endDate, int? accountId)
        {
            try
            {
                // If endDate is not provided, use startDate (daily report)
                DateTime actualEndDate = endDate ?? startDate;

                // Generate report for the given date range
                var reportData = await _reportGenerationService.GenerateReportData(startDate, actualEndDate);

                // Render as text (PDF rendering will be implemented in Phase 5)
                string textReport = _reportGenerationService.RenderReportAsText(reportData);

                // For now, store the text report as a placeholder
                // TODO: Once PDF library is added, convert to PDF here
                byte[] reportContent = System.Text.Encoding.UTF8.GetBytes(textReport);

                // Generate filename based on report type
                string fileName;
                if (startDate.Date == actualEndDate.Date)
                {
                    // Daily report
                    fileName = $"Report_{startDate:yyyy-MM-dd}.pdf";
                }
                else
                {
                    // Monthly or period report
                    fileName = $"Report_{startDate:yyyy-MM-dd}_to_{actualEndDate:yyyy-MM-dd}.pdf";
                }

                // Save to database
                await _reportService.SaveReport(startDate, actualEndDate, reportContent, fileName);

                TempData["SuccessMessage"] = $"Report generated successfully";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error generating report: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadReport(int id)
        {
            try
            {
                var report = await _reportService.GetReportById(id);
                if (report == null)
                {
                    return NotFound();
                }

                return File(report.PDFContent, "application/pdf", report.FileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error downloading report: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ViewReport(int id)
        {
            try
            {
                var report = await _reportService.GetReportById(id);
                if (report == null)
                {
                    return NotFound();
                }

                // Decode the stored text report (currently stored as UTF-8 bytes)
                string reportText = System.Text.Encoding.UTF8.GetString(report.PDFContent);

                ViewBag.ReportId = report.IDReport;
                ViewBag.ReportFileName = report.FileName;
                ViewBag.ReportDate = report.DateCreated;

                return View((object)reportText);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error viewing report: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(List<int> selectedIds)
        {
            try
            {
                if (selectedIds != null && selectedIds.Count > 0)
                {
                    foreach (var id in selectedIds)
                    {
                        await _reportService.DeleteReport(id);
                    }
                    TempData["SuccessMessage"] = $"Deleted {selectedIds.Count} report(s) successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Please select at least one report to delete.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting reports: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}
