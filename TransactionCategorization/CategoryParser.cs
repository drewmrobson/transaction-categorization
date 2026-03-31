namespace TransactionCategorization
{
    internal class CategoryMapping
    {
        public string Match { get; set; }

        public string Category { get; set; }

        public CategoryMapping(string match, string category)
        {
            Match = match;
            Category = category;
        }
    }

    internal class CategoryParser
    {
        const string UncategorisedCategory = "Uncategorised";

        internal void Categorise(List<Model> list, List<CategoryMapping> categories)
        {
            foreach (var l in list)
            {
                l.Category = UncategorisedCategory;
                foreach (var c in categories)
                {
                    if (l.Description.Contains(c.Match, StringComparison.OrdinalIgnoreCase))
                    {
                        l.Category = c.Category;
                    }
                }
            }
        }
    }
}
