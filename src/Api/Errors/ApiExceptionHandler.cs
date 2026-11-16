using Microsoft.AspNetCore.Diagnostics;

namespace Api.Errors;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title) = exception is BadHttpRequestException bad
            ? (bad.StatusCode, "Некоректний HTTP-запит.")
            : (StatusCodes.Status500InternalServerError, "Внутрішня помилка сервера.");
        await Results.Problem(statusCode: status, title: title).ExecuteAsync(context);
        return true;
    }
}
