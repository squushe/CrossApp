using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Import;

public sealed class JsonOrderLineImporter : IOrderLineImporter, IStreamingOrderLineImporter
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<ImportedOrderLine>> LoadAsync(
        string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.Read, 4096, useAsync: true);
        var dtos = await JsonSerializer.DeserializeAsync<List<OrderLineImportDto?>>(
            stream, Options, cancellationToken: ct)
            ?? throw new DomainException($"Файл {Path.GetFileName(path)} не містить списку.");

        var result = new List<ImportedOrderLine>(dtos.Count);
        for (var i = 0; i < dtos.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            result.Add(ConvertRecord(dtos[i], i + 1));
        }
        return result;
    }

    public async IAsyncEnumerable<ImportedOrderLine> ReadAsync(string path,
        IProgress<int>? progress = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.Read, 4096, useAsync: true);
        var index = 0;
        await foreach (var dto in JsonSerializer.DeserializeAsyncEnumerable<OrderLineImportDto?>(
            stream, Options, cancellationToken: ct).WithCancellation(ct))
        {
            ct.ThrowIfCancellationRequested();
            var item = ConvertRecord(dto, ++index);
            // Відсоток прочитаних байтів приблизний через випереджувальну буферизацію JSON.
            // 100% повідомляє сервіс лише після успішного застосування всіх рядків.
            progress?.Report(stream.Length == 0 ? 0 : (int)Math.Min(99, stream.Position * 100d / stream.Length));
            yield return item;
        }
    }

    private static ImportedOrderLine ConvertRecord(OrderLineImportDto? dto, int number)
    {
        if (dto is null)
            throw new DomainException($"Запис {number}: порожній запис.");
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(dto.OrderId);
            var line = OrderLine.Create(dto.ProductId, dto.Name, dto.Price, dto.Quantity);
            return new ImportedOrderLine(dto.OrderId.Trim(), line);
        }
        catch (ArgumentException ex)
        {
            throw new DomainException($"Запис {number}: {ex.Message}", ex);
        }
    }
}
