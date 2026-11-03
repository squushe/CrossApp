using Core.Import;

namespace Core.Abstractions;

public interface IOrderLineImporter
{
    Task<IReadOnlyList<ImportedOrderLine>> LoadAsync(string path, CancellationToken ct = default);
}
