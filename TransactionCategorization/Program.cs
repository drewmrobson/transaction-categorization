using Azure.Identity;
using Azure.Storage.Blobs;
using nietras.SeparatedValues;
using System.CommandLine;
using System.Text;
using System.Text.Json;
using TransactionCategorization;

// 1. File processing command
var fileOption = new Option<FileInfo?>(
    name: "--file",
    description: @"The CSV file to process. e.g. `--file ""C:/Source/Cash.csv""`");

// 2. Update mapping command
var addCategoryOption = new Option<string[]>(
        name: "--add-cat",
        description: @"The transaction and associated category. e.g. `--add-cat ""Karalee Cellars"" ""Alcohol""`"
    )
{
    AllowMultipleArgumentsPerToken = true,
};

// 3. Root config
var rootCommand = new RootCommand("Transaction Categorisation");
rootCommand.AddOption(fileOption);
rootCommand.AddOption(addCategoryOption);

// 4. App handler
rootCommand.SetHandler(async (file, map) =>
{
    var mappingConfig = new MappingConfig();

    // 5. Read config from Azure Blob Storage
    string mappingText = await mappingConfig.Get();

    if (file != null)
    {
        // 6. Process file
        ProcessFile(file!, mappingText);
        return;
    }

    if (map != null
        && map.Length == 2)
    {
        // 7. Update mappiong config
        var categories = JsonSerializer.Deserialize<List<Categories>>(mappingText)!;
        categories.Add(new Categories(map[0], map[1]));

        // Order by alphabetical
        var data = JsonSerializer.Serialize(categories.OrderBy(x => x.Match));
        await mappingConfig.Set(data);
        return;
    }

    throw new ArgumentException("Incorrect arguments provided");
}, fileOption, addCategoryOption);

// 8. Execute app
await rootCommand.InvokeAsync(args);

// Process and categorise a file of transactions
static void ProcessFile(FileInfo file, string mappingConfig)
{
    char defaultSeparator = ',';
    var outputPath = $"C:/Source/{file.Name.Replace(file.Extension, string.Empty)}.processed.csv";

    using var reader = Sep.New(defaultSeparator).Reader().FromFile(file.FullName);
    using var writer = Sep.New(defaultSeparator).Writer().ToFile(outputPath);

    var transactions = new List<Model>();

    foreach (var readRow in reader)
    {
        var date = readRow[0].ToString();
        var amount = readRow[1].ToString().Replace("\"", string.Empty);
        var description = readRow[2].ToString();

        transactions.Add(new Model
        {
            Date = DateTime.Parse(date),
            Amount = decimal.Parse(amount),
            Description = description
        });
    }

    var categories = JsonSerializer.Deserialize<List<Categories>>(mappingConfig)!;
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

public class MappingConfig
{
    public async Task<string> Get()
    {
        var blobServiceClient = new BlobServiceClient(
                        new Uri(Constants.ConnectionString),
                        new DefaultAzureCredential());
        var containerClient = blobServiceClient.GetBlobContainerClient(Constants.ContainerName);
        var blobClient = containerClient.GetBlobClient(Constants.BlobName);
        var download = await blobClient.DownloadAsync();

        using (var reader = new StreamReader(download.Value.Content, Encoding.UTF8))
        {
            return await reader.ReadToEndAsync();
        }
    }

    public async Task Set(string mappingConfig)
    {
        var blobServiceClient = new BlobServiceClient(
                        new Uri(Constants.ConnectionString),
                        new DefaultAzureCredential());
        var containerClient = blobServiceClient.GetBlobContainerClient(Constants.ContainerName);
        var blobClient = containerClient.GetBlobClient(Constants.BlobName);

        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(mappingConfig)))
        {
            await blobClient.UploadAsync(stream, true);
        }
    }
}