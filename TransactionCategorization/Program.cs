using Azure.Identity;
using Azure.Storage.Blobs;
using nietras.SeparatedValues;
using System.CommandLine;
using System.Globalization;
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

// 3. Output file option
var outOption = new Option<FileInfo?>(
    name: "--out",
    description: @"Output file path. Defaults to the input file with '.processed.csv' in the same directory.");

// 4. Root config
var rootCommand = new RootCommand("Transaction Categorisation");
rootCommand.AddOption(fileOption);
rootCommand.AddOption(addCategoryOption);
rootCommand.AddOption(outOption);

// 5. App handler
rootCommand.SetHandler(async (file, map, outFile) =>
{
    var mappingConfig = new MappingConfig();

    if (file != null)
    {
        // 6. Process file
        string mappingText = await mappingConfig.Get();
        ProcessFile(file, mappingText, outFile);
        return;
    }

    if (map != null
        && map.Length == 2)
    {
        // 7. Update mapping config
        string mappingText = await mappingConfig.Get();
        var categories = JsonSerializer.Deserialize<List<Categories>>(mappingText)!;
        categories.Add(new Categories(map[0], map[1]));

        // Order by alphabetical
        var data = JsonSerializer.Serialize(categories.OrderBy(x => x.Match));
        await mappingConfig.Set(data);
        return;
    }

    throw new ArgumentException("Incorrect arguments provided");
}, fileOption, addCategoryOption, outOption);

// 8. Execute app
await rootCommand.InvokeAsync(args);

// Process and categorise a file of transactions
static void ProcessFile(FileInfo file, string mappingConfig, FileInfo? outFile)
{
    char defaultSeparator = ',';
    var outputPath = outFile?.FullName
        ?? Path.Combine(
            file.DirectoryName ?? Environment.CurrentDirectory,
            Path.GetFileNameWithoutExtension(file.Name) + ".processed.csv");
    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

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
            Date = DateTime.Parse(date, CultureInfo.InvariantCulture),
            Amount = decimal.Parse(amount, CultureInfo.InvariantCulture),
            Description = description
        });
    }

    var categories = JsonSerializer.Deserialize<List<Categories>>(mappingConfig)!;
    new CategoryParser().Categorise(transactions, categories);

    foreach (var item in transactions)
    {
        using var writeRow = writer.NewRow();
        writeRow["Date"].Set(item.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        writeRow["Amount"].Set(item.Amount.ToString(CultureInfo.InvariantCulture));
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