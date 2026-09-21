using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace SuryPos.Api.Exceptions;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is ValidationException validationEx)
        {
            var errors = validationEx.Errors
                .GroupBy(e => ValidationErrorKeys.Normalize(e.PropertyName))
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            httpContext.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
            await httpContext.Response.WriteAsJsonAsync(
                new { title = "Validation Error", errors },
                cancellationToken);

            return true;
        }

        // Semua yang lolos validasi tapi gagal di sini = error tak terduga -> 5xx + log.
        logger.LogError(exception, "Terjadi kesalahan: {Message}", exception.Message);

        httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(
            new { title = "Server Error", detail = "Terjadi kesalahan internal pada server." },
            cancellationToken);

        return true;
    }
}