using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Import;

public sealed class JsonOrderLineImporter : IOrderLineImporter
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
            var dto = dtos[i] ?? throw new DomainException($"Запис {i + 1}: порожній запис.");
            try
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(dto.OrderId);
                var line = OrderLine.Create(dto.ProductId, dto.Name, dto.Price, dto.Quantity);
                result.Add(new ImportedOrderLine(dto.OrderId.Trim(), line));
            }
            catch (ArgumentException ex)
            {
                throw new DomainException($"Запис {i + 1}: {ex.Message}", ex);
            }
        }
        return result;
    }
}
