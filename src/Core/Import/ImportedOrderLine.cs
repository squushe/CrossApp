using Core.Domain;

namespace Core.Import;

public sealed record ImportedOrderLine(string OrderId, OrderLine Line);
