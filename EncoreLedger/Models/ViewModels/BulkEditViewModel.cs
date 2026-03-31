namespace EncoreLedger.Models.ViewModels
{
    public class BulkEditViewModel
    {
        public int[] SelectedIds { get; set; }
        public int? CategoryId { get; set; }
        public int? AccountId { get; set; }
        public string? Description { get; set; }
        public string? Notes { get; set; }
    }
}
