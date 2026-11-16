using Api.Contracts;
using Core.Abstractions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrders(this WebApplication app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders");
        group.MapGet("/", async (IAsyncOrderStore store, CancellationToken ct) =>
            TypedResults.Ok((await store.ListAsync(ct)).Select(OrderDto.FromDomain).ToList()))
            .WithSummary("Список замовлень");

        group.MapGet("/{id}", async Task<Results<Ok<OrderDto>, NotFound>>
            (string id, IAsyncOrderStore store, CancellationToken ct) =>
                await store.GetByIdAsync(id, ct) is { } order
                    ? TypedResults.Ok(OrderDto.FromDomain(order)) : TypedResults.NotFound())
            .WithSummary("Замовлення за ID");

        group.MapPost("/", async Task<Results<Created<OrderDto>, ValidationProblem>>
            (SaveOrderRequest request, IAsyncOrderStore store, CancellationToken ct) =>
        {
            if (!request.TryValidate(out var errors)) return TypedResults.ValidationProblem(errors);
            var order = request.ToDomain(Guid.NewGuid().ToString("N"));
            await store.AddAsync(order, ct);
            return TypedResults.Created($"/api/orders/{order.Id}", OrderDto.FromDomain(order));
        }).WithSummary("Створити замовлення");

        group.MapPut("/{id}", async Task<Results<Ok<OrderDto>, NotFound, ValidationProblem>>
            (string id, SaveOrderRequest request, IAsyncOrderStore store, CancellationToken ct) =>
        {
            if (!request.TryValidate(out var errors)) return TypedResults.ValidationProblem(errors);
            var previous = await store.GetByIdAsync(id, ct);
            if (previous is null) return TypedResults.NotFound();
            var order = request.ToDomain(previous.Id, previous);
            return await store.UpdateAsync(order, ct)
                ? TypedResults.Ok(OrderDto.FromDomain(order)) : TypedResults.NotFound();
        }).WithSummary("Замінити клієнта та всі рядки замовлення");

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
