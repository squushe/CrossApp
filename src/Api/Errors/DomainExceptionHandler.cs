using Core;
using Microsoft.AspNetCore.Diagnostics;

namespace Api.Errors;

public sealed class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DomainException) return false;
        // Текст винятку може містити внутрішні деталі: клієнт отримує сталий опис.
        await Results.Problem(statusCode: StatusCodes.Status409Conflict,
            title: "Конфлікт із правилами замовлення.").ExecuteAsync(context);
        return true;
    }
}
