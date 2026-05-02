namespace EncoreLedger.Models
{
    public class Report
    {
        public int IDReport { get; set; }
        public DateTime PeriodStartDate { get; set; }
        public DateTime PeriodEndDate { get; set; }
        public string? FileName { get; set; }
        public byte[]? PDFContent { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
