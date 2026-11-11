using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiBackgroundSalesDemo.Models;
using MauiBackgroundSalesDemo.Services;

namespace MauiBackgroundSalesDemo.ViewModels;

public partial class SalesViewModel : ObservableObject
{
    private readonly LiteDbSalesStore _liteDbSalesStore;
    private readonly SqliteSalesAggregateStore _sqliteSalesAggregateStore;

    public ObservableCollection<string> AvailableProducts { get; } = new()
    {
        "Laptop",
        "Phone",
        "Tablet",
        "Monitor",
        "Keyboard",
        "Headphones",
        "Desk Chair"
    };

    [ObservableProperty]
    private string selectedProduct = "Laptop";

    [ObservableProperty]
    private int quantity = 1;

    [ObservableProperty]
    private decimal price = 999.99m;

    [ObservableProperty]
    private string statusMessage = "Ready to add a new sale.";

    public IAsyncRelayCommand AddTransactionCommand { get; }

    public SalesViewModel(LiteDbSalesStore liteDbSalesStore, SqliteSalesAggregateStore sqliteSalesAggregateStore)
    {
        _liteDbSalesStore = liteDbSalesStore;
        _sqliteSalesAggregateStore = sqliteSalesAggregateStore;
        AddTransactionCommand = new AsyncRelayCommand(AddTransactionAsync);
        Price = GetSuggestedPrice(SelectedProduct);
    }

    partial void OnSelectedProductChanged(string value)
    {
        Price = GetSuggestedPrice(value);
    }

    private async Task AddTransactionAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedProduct))
        {
            StatusMessage = "Choose a product before saving.";
            return;
        }

        if (Quantity <= 0)
        {
            StatusMessage = "Quantity must be greater than zero.";
            return;
        }

        if (Price <= 0)
        {
            StatusMessage = "Price must be greater than zero.";
            return;
        }

        var transaction = new ProductSaleTransaction
        {
            Timestamp = DateTime.Now,
            Product = SelectedProduct,
            Quantity = Quantity,
            Price = Price
        };

        await _liteDbSalesStore.AddTransactionAsync(transaction);
        var allTransactions = await _liteDbSalesStore.GetTransactionsAsync();
        await _sqliteSalesAggregateStore.UpdateFromTransactionsAsync(allTransactions);

        StatusMessage = $"Saved {transaction.Product} x {transaction.Quantity} for {transaction.Total:C}.";
        Quantity = 1;
        Price = GetSuggestedPrice(SelectedProduct);
    }

    private static decimal GetSuggestedPrice(string product)
    {
        return product switch
        {
            "Laptop" => 999.99m,
            "Phone" => 699.00m,
            "Tablet" => 449.50m,
            "Monitor" => 329.99m,
            "Keyboard" => 79.99m,
            "Headphones" => 129.00m,
            "Desk Chair" => 245.00m,
            _ => 49.99m
        };
    }
}