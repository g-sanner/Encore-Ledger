namespace EncoreLedger.Models
{
    public class ImportMapping
    {
        public int IDImportMapping { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? AccountID { get; set; }
        
        // Column indices for mapping
        public int? DateIndex { get; set; }
        public int? DescriptionIndex { get; set; }
        public int? AmountIndex { get; set; }
        public int? DebitIndex { get; set; }
        public int? CreditIndex { get; set; }
        
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }

        public Account? Account { get; set; }
    }
}
