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
        var categories = JsonSerializer.Deserialize<List<CategoryMapping>>(mappingText)
            ?? throw new InvalidOperationException("Failed to deserialize category mappings.");
        categories.Add(new CategoryMapping(map[0], map[1]));

        // Order by alphabetical
        var data = JsonSerializer.Serialize(categories.OrderBy(x => x.Match));
        await mappingConfig.Set(data);
        return;
    }

    Console.Error.WriteLine("Error: either --file or --add-cat must be provided.");
    Environment.ExitCode = 1;
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

    int lineNumber = 0;
    foreach (var readRow in reader)
    {
        lineNumber++;
        if (readRow.ColCount < 3)
        {
            Console.Error.WriteLine($"Row {lineNumber}: skipped — expected 3 columns, found {readRow.ColCount}.");
            continue;
        }

        var dateRaw = readRow[0].ToString();
        var amountRaw = readRow[1].ToString().Replace("\"", string.Empty);
        var description = readRow[2].ToString();

        if (!DateTime.TryParse(dateRaw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            Console.Error.WriteLine($"Row {lineNumber}: skipped — could not parse date '{dateRaw}'.");
            continue;
        }

        if (!decimal.TryParse(amountRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
        {
            Console.Error.WriteLine($"Row {lineNumber}: skipped — could not parse amount '{amountRaw}'.");
            continue;
        }

        transactions.Add(new Model
        {
            Date = date,
            Amount = amount,
            Description = description
        });
    }

    var categories = JsonSerializer.Deserialize<List<CategoryMapping>>(mappingConfig)
        ?? throw new InvalidOperationException("Failed to deserialize category mappings.");
    new CategoryParser().Categorise(transactions, categories);

    foreach (var item in transactions)
    {
        using var writeRow = writer.NewRow();
        writeRow["Date"].Set(item.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        writeRow["Amount"].Set(item.Amount.ToString(CultureInfo.InvariantCulture));
        writeRow["Description"].Set(item.Description);
        writeRow["Category"].Set(item.Category);
    }
}

public class MappingConfig
{
    private readonly BlobClient _blobClient;

    public MappingConfig()
    {
        var blobServiceClient = new BlobServiceClient(
            new Uri(Constants.ConnectionString),
            new DefaultAzureCredential());
        _blobClient = blobServiceClient
            .GetBlobContainerClient(Constants.ContainerName)
            .GetBlobClient(Constants.BlobName);
    }

    public async Task<string> Get()
    {
        var download = await _blobClient.DownloadAsync();
        using (var reader = new StreamReader(download.Value.Content, Encoding.UTF8))
        {
            return await reader.ReadToEndAsync();
        }
    }

    public async Task Set(string mappingConfig)
    {
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(mappingConfig)))
        {
            await _blobClient.UploadAsync(stream, true);
        }
    }
}