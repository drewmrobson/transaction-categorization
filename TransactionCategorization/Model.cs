namespace TransactionCategorization
{
    internal class Model
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = "Unset";
    }
}
