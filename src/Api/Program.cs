using Api.Contracts;
using Api.Endpoints;
using Api.Errors;
using Api.Validation;
using Core.Abstractions;
using Core.Storage;
using FluentValidation;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
// Некоректний JSON також проходить через стандартний обробник ProblemDetails.
builder.Services.Configure<Microsoft.AspNetCore.Routing.RouteHandlerOptions>(options =>
    options.ThrowOnBadRequest = true);
builder.Services.AddScoped<IValidator<SaveOrderRequest>, SaveOrderRequestValidator>();
builder.Services.AddSingleton<IAsyncOrderStore, AsyncInMemoryOrderStore>();
var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
app.MapGet("/health", () => TypedResults.Ok(new { status = "ok" }));
app.MapOrders();
app.Run();

// Доступ до точки входу для WebApplicationFactory у tests/Api.Tests.
public partial class Program { }
