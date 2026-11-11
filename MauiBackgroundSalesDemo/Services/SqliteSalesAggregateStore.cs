using MauiBackgroundSalesDemo.Models;
using Microsoft.Data.Sqlite;

namespace MauiBackgroundSalesDemo.Services;

public sealed class SqliteSalesAggregateStore
{
    private readonly string _databasePath;

    public SqliteSalesAggregateStore(string databasePath)
    {
        _databasePath = databasePath;
        var folder = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(folder))
        {
            Directory.CreateDirectory(folder);
        }
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS DailySalesSummary (
                    DateKey TEXT PRIMARY KEY,
                    TotalRevenue REAL NOT NULL,
                    TransactionCount INTEGER NOT NULL
                );

                CREATE TABLE IF NOT EXISTS ProductSalesSummary (
                    Product TEXT PRIMARY KEY,
                    TotalRevenue REAL NOT NULL,
                    UnitsSold INTEGER NOT NULL
                );

                CREATE TABLE IF NOT EXISTS CategorySalesSummary (
                    Category TEXT PRIMARY KEY,
                    TotalRevenue REAL NOT NULL,
                    UnitsSold INTEGER NOT NULL
                );";

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task UpdateFromTransactionsAsync(IEnumerable<ProductSaleTransaction> transactions, CancellationToken cancellationToken = default)
    {
        var allTransactions = transactions.ToList();
        var daily = allTransactions
            .GroupBy(x => x.Timestamp.Date)
            .Select(g => new DailySalesSummaryItem
            {
                DateKey = g.Key.ToString("yyyy-MM-dd"),
                TotalRevenue = g.Sum(x => x.Total),
                TransactionCount = g.Count()
            })
            .OrderBy(x => x.DateKey)
            .ToList();

        var products = allTransactions
            .GroupBy(x => x.Product)
            .Select(g => new ProductSalesSummaryItem
            {
                Product = g.Key,
                TotalRevenue = g.Sum(x => x.Total),
                UnitsSold = g.Sum(x => x.Quantity)
            })
            .OrderByDescending(x => x.TotalRevenue)
            .ToList();

        var categories = allTransactions
            .GroupBy(x => GetCategory(x.Product))
            .Select(g => new CategorySalesSummaryItem
            {
                Category = g.Key,
                TotalRevenue = g.Sum(x => x.Total),
                UnitsSold = g.Sum(x => x.Quantity)
            })
            .OrderByDescending(x => x.TotalRevenue)
            .ToList();

        await using var connection = await OpenConnectionAsync(cancellationToken);

        await using (var clearDaily = connection.CreateCommand())
        {
            clearDaily.CommandText = "DELETE FROM DailySalesSummary;";
            await clearDaily.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var clearProducts = connection.CreateCommand())
        {
            clearProducts.CommandText = "DELETE FROM ProductSalesSummary;";
            await clearProducts.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var clearCategories = connection.CreateCommand())
        {
            clearCategories.CommandText = "DELETE FROM CategorySalesSummary;";
            await clearCategories.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var item in daily)
        {
            await using var insertDaily = connection.CreateCommand();
            insertDaily.CommandText = @"
                INSERT INTO DailySalesSummary (DateKey, TotalRevenue, TransactionCount)
                VALUES (@DateKey, @TotalRevenue, @TransactionCount);";
            insertDaily.Parameters.AddWithValue("@DateKey", item.DateKey);
            insertDaily.Parameters.AddWithValue("@TotalRevenue", Convert.ToDouble(item.TotalRevenue));
            insertDaily.Parameters.AddWithValue("@TransactionCount", item.TransactionCount);
            await insertDaily.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var item in products)
        {
            await using var insertProducts = connection.CreateCommand();
            insertProducts.CommandText = @"
                INSERT INTO ProductSalesSummary (Product, TotalRevenue, UnitsSold)
                VALUES (@Product, @TotalRevenue, @UnitsSold);";
            insertProducts.Parameters.AddWithValue("@Product", item.Product);
            insertProducts.Parameters.AddWithValue("@TotalRevenue", Convert.ToDouble(item.TotalRevenue));
            insertProducts.Parameters.AddWithValue("@UnitsSold", item.UnitsSold);
            await insertProducts.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var item in categories)
        {
            await using var insertCategories = connection.CreateCommand();
            insertCategories.CommandText = @"
                INSERT INTO CategorySalesSummary (Category, TotalRevenue, UnitsSold)
                VALUES (@Category, @TotalRevenue, @UnitsSold);";
            insertCategories.Parameters.AddWithValue("@Category", item.Category);
            insertCategories.Parameters.AddWithValue("@TotalRevenue", Convert.ToDouble(item.TotalRevenue));
            insertCategories.Parameters.AddWithValue("@UnitsSold", item.UnitsSold);
            await insertCategories.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task<List<DailySalesSummaryItem>> GetDailySalesAsync(CancellationToken cancellationToken = default)
    {
        var rows = new List<DailySalesSummaryItem>();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT DateKey, TotalRevenue, TransactionCount FROM DailySalesSummary ORDER BY DateKey ASC;";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new DailySalesSummaryItem
            {
                DateKey = reader.GetString(0),
                TotalRevenue = Convert.ToDecimal(reader.GetDouble(1)),
                TransactionCount = reader.GetInt32(2)
            });
        }

        return rows;
    }

    public async Task<List<ProductSalesSummaryItem>> GetProductSalesAsync(CancellationToken cancellationToken = default)
    {
        var rows = new List<ProductSalesSummaryItem>();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Product, TotalRevenue, UnitsSold FROM ProductSalesSummary ORDER BY TotalRevenue DESC;";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new ProductSalesSummaryItem
            {
                Product = reader.GetString(0),
                TotalRevenue = Convert.ToDecimal(reader.GetDouble(1)),
                UnitsSold = reader.GetInt32(2)
            });
        }

        return rows;
    }

    public async Task<List<CategorySalesSummaryItem>> GetCategorySalesAsync(CancellationToken cancellationToken = default)
    {
        var rows = new List<CategorySalesSummaryItem>();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Category, TotalRevenue, UnitsSold FROM CategorySalesSummary ORDER BY TotalRevenue DESC;";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new CategorySalesSummaryItem
            {
                Category = reader.GetString(0),
                TotalRevenue = Convert.ToDecimal(reader.GetDouble(1)),
                UnitsSold = reader.GetInt32(2)
            });
        }

        return rows;
    }

    private static string GetCategory(string product)
    {
        var normalized = product.Trim();

        if (normalized.Contains("Laptop", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("Phone", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("Tablet", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("Monitor", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("Keyboard", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("Mouse", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("Headphones", StringComparison.OrdinalIgnoreCase))
        {
            return "Electronics";
        }

        if (normalized.Contains("Desk", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("Chair", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("Lamp", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("Shelf", StringComparison.OrdinalIgnoreCase))
        {
            return "Office Furniture";
        }

        if (normalized.Contains("Shirt", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("Jacket", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("Sneakers", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("Hat", StringComparison.OrdinalIgnoreCase))
        {
            return "Apparel";
        }

        return "General";
    }

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}