using nietras.SeparatedValues;
using TransactionCategorization;

using var reader = Sep.New(',').Reader().FromFile("C:/Source/CSVData.csv");
using var writer = Sep.New(',').Writer().ToFile("C:/Source/output.csv");

var list = new List<Model>();

foreach (var readRow in reader)
{
    var date = readRow[0].ToString();
    var amount = readRow[1].ToString().Replace("\"", "");
    var description = readRow[2].ToString();

    var l = new Model
    {
        Date = DateTime.Parse(date),
        Amount = decimal.Parse(amount),
        Description = description
    };
    list.Add(l);

    Console.WriteLine($"{l.Date} {l.Amount} {l.Description}");
}

var categories = new List<Categories>()
{
    new Categories("Tatts", "Gambling"),
    new Categories("Transfer to CBA A/c NetBank Rent", "Rent"),
    new Categories("NERO UTILITIES", "Utilities"),
    new Categories("Club Bunker", "Gym")
};

new CategoryParser().Categorise(list, categories);

foreach (var w in list)
{
    using var writeRow = writer.NewRow();
    writeRow["Date"].Set(w.Date.ToString());
    writeRow["Amount"].Set(w.Amount.ToString());
    writeRow["Description"].Set(w.Description);
    writeRow["Category"].Set(w.Category);
}

writer.Dispose();