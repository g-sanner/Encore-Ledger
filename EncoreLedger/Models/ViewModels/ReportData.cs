namespace EncoreLedger.Models.ViewModels
{
    public class ReportData 
    {
        public DateTime PeriodStartDate { get; set; }
        public DateTime PeriodEndDate { get; set; }
        public ReportSummary? Summary { get; set; }
        public List<CategoryBreakdown> CategoryBreakdowns { get; set; } = new List<CategoryBreakdown>();
    }
}
