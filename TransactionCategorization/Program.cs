using nietras.SeparatedValues;
using System.CommandLine;
using System.Text.Json;
using TransactionCategorization;

// 1. File processing command
var fileOption = new Option<FileInfo?>(
    name: "--file",
    description: "The file to read and display on the console.");

// 2. Update mapping command
var addCategoryOption = new Option<string[]>(
        name: "--add-cat",
        description: "The category to add"
    )
{
    AllowMultipleArgumentsPerToken = true,
};

// 3. Root config
var rootCommand = new RootCommand("Sample app for System.CommandLine");
rootCommand.AddOption(fileOption);
rootCommand.AddOption(addCategoryOption);

rootCommand.SetHandler((file, map) =>
{
    if(file != null)
    {
        ProcessFile(file!);
    }

    if(map != null
        && map.Length == 2)
    {
        string mappingText = File.ReadAllText("C:/Source/mapping.json");
        var categories = JsonSerializer.Deserialize<List<Categories>>(mappingText);
        categories.Add(new Categories(map[0], map[1]));
        var data = JsonSerializer.Serialize(categories);
        File.WriteAllText("C:/Source/mapping.json", data);
    }
}, fileOption, addCategoryOption);

await rootCommand.InvokeAsync(args);

static void ProcessFile(FileInfo file)
{
    using var reader = Sep.New(',').Reader().FromFile(file.FullName);
    using var writer = Sep.New(',').Writer().ToFile("C:/Source/output.csv");

    var transactions = new List<Model>();

    foreach (var readRow in reader)
    {
        var date = readRow[0].ToString();
        var amount = readRow[1].ToString().Replace("\"", "");
        var description = readRow[2].ToString();

        transactions.Add(new Model
        {
            Date = DateTime.Parse(date),
            Amount = decimal.Parse(amount),
            Description = description
        });
    }

    var json = File.ReadAllText("C:/Source/mapping.json");
    var categories = JsonSerializer.Deserialize<List<Categories>>(json);
    new CategoryParser().Categorise(transactions, categories);

    foreach (var item in transactions)
    {
        using var writeRow = writer.NewRow();
        writeRow["Date"].Set(item.Date.ToString());
        writeRow["Amount"].Set(item.Amount.ToString());
        writeRow["Description"].Set(item.Description);
        writeRow["Category"].Set(item.Category);
    }

    writer.Dispose();
}