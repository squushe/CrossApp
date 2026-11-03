using Core.Import;

namespace Core.Abstractions;

public interface IStreamingOrderLineImporter
{
    IAsyncEnumerable<ImportedOrderLine> ReadAsync(string path,
        IProgress<int>? progress = null, CancellationToken ct = default);
}
