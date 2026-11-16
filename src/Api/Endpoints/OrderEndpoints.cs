using Api.Contracts;
using Api.Validation;
using Core.Abstractions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrders(this WebApplication app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders");
        group.MapGet("/", async Task<Results<Ok<PagedResult<OrderDto>>, ValidationProblem>>
            (IAsyncOrderStore store, CancellationToken ct, int page = 1, int pageSize = 20, string? search = null) =>
        {
            var errors = new Dictionary<string, string[]>();
            if (page < 1) errors["page"] = ["Номер сторінки має бути не меншим за 1."];
            if (pageSize is < 1 or > 100) errors["pageSize"] = ["Розмір сторінки: від 1 до 100."];
            if (search?.Length > 200) errors["search"] = ["Пошук: до 200 символів."];
            if (errors.Count > 0) return TypedResults.ValidationProblem(errors);
            var orders = await store.ListAsync(ct);
            var query = orders.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(order =>
                    order.Id.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    order.CustomerId.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    order.Lines.Any(line => line.ProductId.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        line.Name.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }
            var filtered = query.OrderBy(order => order.CreatedAt)
                .ThenBy(order => order.Id, StringComparer.Ordinal).ToList();
            var skip = (int)Math.Min((page - 1L) * pageSize, int.MaxValue);
            var items = filtered.Skip(skip).Take(pageSize).Select(OrderDto.FromDomain).ToList();
            return TypedResults.Ok(new PagedResult<OrderDto>(items, filtered.Count));
        }).WithSummary("Замовлення: пагінація та пошук");

        group.MapGet("/{id}", async Task<Results<Ok<OrderDto>, NotFound>>
            (string id, IAsyncOrderStore store, CancellationToken ct) =>
                await store.GetByIdAsync(id, ct) is { } order
                    ? TypedResults.Ok(OrderDto.FromDomain(order)) : TypedResults.NotFound())
            .WithSummary("Замовлення за ID");

        group.MapPost("/", async Task<Results<Created<OrderDto>, ValidationProblem>>
            (SaveOrderRequest request, IAsyncOrderStore store, CancellationToken ct) =>
        {
            var order = request.ToDomain(Guid.NewGuid().ToString("N"));
            await store.AddAsync(order, ct);
            return TypedResults.Created($"/api/orders/{order.Id}", OrderDto.FromDomain(order));
        }).AddEndpointFilter<ValidationFilter<SaveOrderRequest>>()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Створити замовлення");

        group.MapPut("/{id}", async Task<Results<Ok<OrderDto>, NotFound, ValidationProblem>>
            (string id, SaveOrderRequest request, IAsyncOrderStore store, CancellationToken ct) =>
        {
            var previous = await store.GetByIdAsync(id, ct);
            if (previous is null) return TypedResults.NotFound();
            var order = request.ToDomain(previous.Id, previous);
            return await store.UpdateAsync(order, ct)
                ? TypedResults.Ok(OrderDto.FromDomain(order)) : TypedResults.NotFound();
        }).AddEndpointFilter<ValidationFilter<SaveOrderRequest>>()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Замінити клієнта та всі рядки замовлення");

        group.MapDelete("/{id}", async Task<Results<NoContent, NotFound>>
            (string id, IAsyncOrderStore store, CancellationToken ct) =>
                await store.RemoveAsync(id, ct) ? TypedResults.NoContent() : TypedResults.NotFound())
            .WithSummary("Видалити замовлення");

        group.MapGet("/{id}/total", async Task<Results<Ok<OrderTotalDto>, NotFound>>
            (string id, IAsyncOrderStore store, CancellationToken ct) =>
                await store.GetByIdAsync(id, ct) is { } order
                    ? TypedResults.Ok(new OrderTotalDto(order.Id, order.Total)) : TypedResults.NotFound())
            .WithSummary("Сума замовлення");
    }
}
