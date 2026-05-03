namespace EncoreLedger.Models.ViewModels
{ 
    public class CategoryBreakdown
    {
        public string? CategoryName { get; set; }
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
        public decimal Net { get; set; }
    }
}
