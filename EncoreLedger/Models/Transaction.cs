namespace EncoreLedger.Models
{
    public class Transaction
    {
        public int IDTransaction { get; set; }
        public string? Description { get; set; }
        public double Amount { get; set; }
        public string? Notes { get; set; }
        public string? InsertType { get; set; }

        public DateTime TransactionDate { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateEdited { get; set; }

        public int? CategoryID { get; set; }
        public Category? Category { get; set; }

        public int? AccountID { get; set; }
        public Account? Account { get; set; }

        public int? BulkImportID { get; set; }
        public BulkImport? BulkImport { get; set; }
    }
}