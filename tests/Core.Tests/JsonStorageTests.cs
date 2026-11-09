using Core.Configuration;
using Core.Storage;

namespace Core.Tests;

public sealed class JsonStorageTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "crossapp-tests-" + Guid.NewGuid().ToString("N"));
    private string FilePath => Path.Combine(_directory, "nested", "orders.json");

    [Fact]
    public void RoundTrip_PreservesCyrillicLinesTotalsAndStatus()
    {
        var confirmed = TestData.Order("CONFIRMED", 12.50m, 2);
        confirmed.AddLine("P-UA", "Український товар", 7m, 3);
        confirmed.Confirm();
        var cancelled = TestData.Order("CANCELLED");
        cancelled.Cancel();
        OrderJsonFile.Write(FilePath, [confirmed, cancelled]);
        var restored = new FileOrderStore(FilePath).List();
        Assert.Equal(2, restored.Count);
        Assert.Equal(confirmed.Id, restored[0].Id);
        Assert.Equal(confirmed.CustomerId, restored[0].CustomerId);
        Assert.Equal(confirmed.Status, restored[0].Status);
        Assert.Equal(confirmed.Lines.Select(l => l.ToDto()), restored[0].Lines.Select(l => l.ToDto()));
        Assert.Equal(46m, restored[0].Total);
        Assert.Equal(cancelled.Status, restored[1].Status);
        var json = File.ReadAllText(FilePath);
        Assert.Contains("Український товар", json);
        Assert.Contains("\"productId\"", json);
        Assert.DoesNotContain("\\u", json);
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(FilePath)!, "*.tmp"));
    }

    [Fact]
    public void InvalidCollection_DoesNotOverwriteExistingFile()
    {
        OrderJsonFile.Write(FilePath, [TestData.Order()]);
        var before = File.ReadAllText(FilePath);
        var input = Path.Combine(_directory, "invalid.json");
        File.WriteAllText(input, "[{\"id\":\"X\",\"customerId\":\"C\",\"lines\":[],\"status\":\"Unknown\"}]");
        Assert.Throws<DomainException>(() => OrderJsonFile.Write(FilePath, OrderJsonFile.Read(input)));
        Assert.Equal(before, File.ReadAllText(FilePath));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[null]")]
    [InlineData("[{\"id\":\"X\",\"customerId\":\"C\",\"lines\":null,\"status\":\"Draft\"}]")]
    [InlineData("[{\"id\":\"X\",\"customerId\":\"C\",\"lines\":[],\"status\":\"Draft\"},{\"id\":\"x\",\"customerId\":\"C\",\"lines\":[],\"status\":\"Draft\"}]")]
    public void Read_RejectsInvalidDomainData(string json)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, json);
        Assert.Throws<DomainException>(() => OrderJsonFile.Read(FilePath));
    }

    [Fact]
    public void FileStore_RewritesEntireCollectionAndReadsLegacyPascalCase()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, "[{\"Id\":\"OLD\",\"CustomerId\":\"C\",\"Lines\":[],\"Status\":\"Draft\"}]");
        var store = new FileOrderStore(FilePath);
        Assert.NotNull(store.GetById("old"));
        store.Add(TestData.Order("NEW"));
        Assert.True(store.Remove("OLD"));
        var restored = new FileOrderStore(FilePath);
        Assert.Single(restored.List());
        Assert.Equal("NEW", restored.List()[0].Id);
        OrderJsonFile.Write(FilePath, []);
        Assert.Empty(new FileOrderStore(FilePath).List());
    }

    [Fact]
    public void ResolveFullPath_UsesExplicitBaseAndHonorsAbsoluteDirectory()
    {
        var options = new StorageOptions { DataPath = "data", FileName = "orders.json" };
        Assert.Equal(Path.Combine(_directory, "data", "orders.json"), options.ResolveFullPath(_directory));
        options.DataPath = _directory;
        Assert.Equal(Path.Combine(_directory, "orders.json"), options.ResolveFullPath(AppContext.BaseDirectory));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }
}
