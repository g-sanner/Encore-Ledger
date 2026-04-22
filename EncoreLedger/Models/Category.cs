namespace EncoreLedger.Models
{
    public class Category
    {
        public int IDCategory { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }

        public DateTime DateCreated { get; set; }
        public DateTime DateEdited { get; set; }

        public ICollection<Transaction>? Transactions { get; set; }
    }
}
