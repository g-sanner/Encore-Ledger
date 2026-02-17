namespace EncoreLedger.Models
{
    public class BulkImport
    {
        public int IDBulkImport { get; set; }
        public string? FileName { get; set; }
        public DateTime ImportDate { get; set; }
        public int? TotalRecords { get; set; }
        public int? RecordsImported { get; set; }
        public int? RecordsFailed { get; set; }

        public ICollection<Transaction>? Transactions { get; set; }
    }
}