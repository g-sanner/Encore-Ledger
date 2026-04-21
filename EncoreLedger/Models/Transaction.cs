using System.ComponentModel.DataAnnotations;

namespace EncoreLedger.Models
{
    public class Transaction
    {
        public int IDTransaction { get; set; }
        public string? Description { get; set; }

        [Range(typeof(decimal), "-1000000000", "1000000000", ErrorMessage = "Enter a valid dollar amount.")]
        [RegularExpression(@"^-?\d+(\.\d{1,2})?$", ErrorMessage = "Amount must have at most 2 decimal places.")]
        public decimal Amount { get; set; }
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