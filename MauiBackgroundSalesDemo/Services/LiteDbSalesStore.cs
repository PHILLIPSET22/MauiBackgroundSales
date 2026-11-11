using LiteDB;
using MauiBackgroundSalesDemo.Models;

namespace MauiBackgroundSalesDemo.Services;

public sealed class LiteDbSalesStore
{
    private readonly string _databasePath;

    public LiteDbSalesStore(string databasePath)
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
        await Task.Run(() =>
        {
            using var database = new LiteDatabase(_databasePath);
            var sales = database.GetCollection<ProductSaleTransaction>("sales");
            sales.EnsureIndex(x => x.Timestamp);
            sales.EnsureIndex(x => x.Product);
        }, cancellationToken);
    }

    public async Task<List<ProductSaleTransaction>> GetTransactionsAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            using var database = new LiteDatabase(_databasePath);
            return database
                .GetCollection<ProductSaleTransaction>("sales")
                .FindAll()
                .OrderByDescending(x => x.Timestamp)
                .ToList();
        }, cancellationToken);
    }

    public async Task AddTransactionAsync(ProductSaleTransaction transaction, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transaction.Product))
        {
            throw new InvalidOperationException("Transaction product cannot be empty.");
        }

        await Task.Run(() =>
        {
            using var database = new LiteDatabase(_databasePath);
            var sales = database.GetCollection<ProductSaleTransaction>("sales");
            sales.Insert(transaction);
        }, cancellationToken);
    }

    public async Task EnsureSeedDataAsync(CancellationToken cancellationToken = default)
    {
        var transactions = await GetTransactionsAsync(cancellationToken);
        if (transactions.Count > 0)
        {
            return;
        }

        var starterData = new[]
        {
            new ProductSaleTransaction { Timestamp = DateTime.Now.AddDays(-5), Product = "Laptop", Quantity = 2, Price = 999.99m },
            new ProductSaleTransaction { Timestamp = DateTime.Now.AddDays(-4), Product = "Phone", Quantity = 4, Price = 699.00m },
            new ProductSaleTransaction { Timestamp = DateTime.Now.AddDays(-3), Product = "Monitor", Quantity = 1, Price = 329.50m },
            new ProductSaleTransaction { Timestamp = DateTime.Now.AddDays(-2), Product = "Keyboard", Quantity = 3, Price = 79.99m },
            new ProductSaleTransaction { Timestamp = DateTime.Now.AddDays(-1), Product = "Headphones", Quantity = 5, Price = 129.00m },
            new ProductSaleTransaction { Timestamp = DateTime.Now, Product = "Laptop", Quantity = 1, Price = 1249.00m },
            new ProductSaleTransaction { Timestamp = DateTime.Now, Product = "Desk Chair", Quantity = 2, Price = 245.00m },
            new ProductSaleTransaction { Timestamp = DateTime.Now.AddHours(-6), Product = "Phone", Quantity = 3, Price = 719.00m }
        };

        foreach (var transaction in starterData)
        {
            await AddTransactionAsync(transaction, cancellationToken);
        }
    }
}