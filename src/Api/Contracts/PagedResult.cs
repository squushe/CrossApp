namespace Api.Contracts;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total);
