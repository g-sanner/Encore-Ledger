namespace EncoreLedger.Models
{
    public class Account
    {
        public int IDAccount { get; set; }
        public string? AccountName { get; set; }
        public string? AccountType { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateEdited { get; set; }

        public ICollection<Transaction>? Transactions { get; set; }
    }
}
