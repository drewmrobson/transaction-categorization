namespace TransactionCategorization
{
    internal class CategoryParser
    {
        internal void Categorise(List<Model> list)
        {
            foreach (var w in list)
            {
                w.Category = "Uncategorised";
                if (w.Description.Contains("Tatts Online"))
                {
                    w.Category = "Gambling";
                }
            }
        }
    }
}
