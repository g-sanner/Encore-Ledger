namespace EncoreLedger.Models.ViewModels
{
    public class ImportPreviewViewModel
    {
        public int AccountID { get; set; }
        public string? FileName { get; set; }


        public List<string> Headers { get; set; } = new();
        public List<string[]> PreviewRows { get; set; } = new();

        // Mapping selections
        public Dictionary<int, string> ColumnMappings { get; set; } = new();

        public int? DateIndex { get; set; }
        public int? DescriptionIndex { get; set; }
        public int? AmountIndex { get; set; }
        public int? DebitIndex { get; set; }
        public int? CreditIndex { get; set; }

        // "Skip" or "UseToday"
        public string PendingHandling { get; set; } = "Skip";

        // Raw data stored temporarily
        public string? SerializedRows { get; set; }
    }
}