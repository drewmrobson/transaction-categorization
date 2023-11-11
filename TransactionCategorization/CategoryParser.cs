namespace TransactionCategorization
{
    internal class Categories
    {
        public string Match { get; set; }
        public string Category { get; set; }

        public Categories(string match, string category)
        {
            Match = match;
            Category = category;
        }
    }

    internal class CategoryParser
    {
        internal void Categorise(List<Model> list, List<Categories> categories)
        {
            foreach (var l in list)
            {
                l.Category = "Uncategorised";
                foreach (var c in categories)
                {
                    if (l.Description.Contains(c.Match))
                    {
                        l.Category = c.Category;
                    }
                }
            }
        }
    }
}
