namespace EncoreLedger.Models.ViewModels
{
    public class TransactionIndexViewModel
    {
        public List<Transaction> Transactions { get; set; } = new();

        public bool DisplayAccountName { get; set; }

        public string SortColumn { get; set; } = "Date";

        public bool Ascending { get; set; } = true;

        public int TotalTransactionCount { get; set; }

        public DateTime? LastDateUpdated { get; set; }
    }
}