# transaction-categorization
Categorise and auto-map your financial transactions.

I built this dumb little app for myself and maybe you can find it useful too?

- [Overview](#overview)
- [Dependencies](#dependencies)
- [Build](#build)
- [Usage](#Usage)

## Overview

Transaction Categorization is a CLI tool for bulk mapping financial transactions in a CSV file. I built this tool to make managing my finances and tax easier, since there are a lot of common transactions in my financial records.

## Dependencies

1. `Sep` and `System.CommandLine` from NuGet
2. A CSV of your financial transactions with the following headers:
| Date  | Amount | Description |
|-------|--------|-------------|

## Build

```bash
cd C:/Source
git clone https://github.com/drewmrobson/transaction-categorization.git
cd C:/Source/transaction-categorization/TransactionCategorization
dotnet publish -o "C:/Source/transactioncategorization"
```

## Usage

The following will:

1. Add a category map for any transaction with a partial match to 'Woolworths' to the category of 'Groceries'.
2. Run the categorization process over the file, adding categories and creating a new file as the result.

```bash
cd C:/Source/transactioncategorization
TransactionCategorization.exe --add-cat "Woolworths" "Groceries"
TransactionCategorization.exe --file "C:/Source/Spending.csv"
```

### File Format

The CSV file is read into this model:

```csharp
internal class Model
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = "Unset";
    public string Category { get; set; } = "Unset";
}
```
