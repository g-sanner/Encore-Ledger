namespace EncoreLedger.Models
{
    public class Transaction
    {
        public int IDTransaction { get; set; }
        public int CategoryID { get; set; }
        public double Amount { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CategoryName { get; set; }
    }
}
