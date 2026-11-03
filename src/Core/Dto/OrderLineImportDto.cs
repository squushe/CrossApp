namespace Core.Dto;

public sealed record OrderLineImportDto(
    string OrderId, string ProductId, string Name, decimal Price, int Quantity);
