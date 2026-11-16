using Api.Endpoints;
using Core;
using Core.Abstractions;
using Core.Storage;
using Microsoft.AspNetCore.Diagnostics;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
// Некоректний JSON також проходить через стандартний обробник ProblemDetails.
builder.Services.Configure<Microsoft.AspNetCore.Routing.RouteHandlerOptions>(options =>
    options.ThrowOnBadRequest = true);
builder.Services.AddSingleton<IAsyncOrderStore, AsyncInMemoryOrderStore>();
var app = builder.Build();
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    if (error is OperationCanceledException && context.RequestAborted.IsCancellationRequested) return;
    var (status, title) = error switch
    {
        BadHttpRequestException bad => (bad.StatusCode, "Некоректний HTTP-запит."),
        DomainException => (StatusCodes.Status400BadRequest, "Операція порушує правила замовлення."),
        _ => (StatusCodes.Status500InternalServerError, "Внутрішня помилка сервера.")
    };
    await Results.Problem(statusCode: status, title: title).ExecuteAsync(context);
}));
app.UseStatusCodePages();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
app.MapGet("/health", () => TypedResults.Ok(new { status = "ok" }));
app.MapOrders();
app.Run();
