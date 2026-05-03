using EncoreLedger.Data;
using EncoreLedger.Models;
using Microsoft.EntityFrameworkCore;

namespace EncoreLedger.Services
{
    public class ReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// Saves a report to the database
        public async Task<Report> SaveReport(DateTime periodStartDate, DateTime periodEndDate, byte[] pdfContent, string fileName)
        {
            var report = new Report
            {
                PeriodStartDate = periodStartDate,
                PeriodEndDate = periodEndDate,
                FileName = fileName,
                PDFContent = pdfContent,
                DateCreated = DateTime.Now
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            return report;
        }

        // Retrieves all reports, filtered by date range if provided
        public async Task<List<Report>> GetReports(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Reports.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(r => r.DateCreated >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(r => r.DateCreated <= endDate.Value);

            return await query.OrderByDescending(r => r.DateCreated).ToListAsync();
        }

        // Retrieve a single report by ID
        public async Task<Report?> GetReportById(int reportId)
        {
            return await _context.Reports.FirstOrDefaultAsync(r => r.IDReport == reportId);
        }

        // Deletes a report by ID
        public async Task DeleteReport(int reportId)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report != null)
            {
                _context.Reports.Remove(report);
                await _context.SaveChangesAsync();
            }
        }
    }
}
