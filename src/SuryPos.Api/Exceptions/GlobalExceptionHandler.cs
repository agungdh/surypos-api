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
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            httpContext.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
            await httpContext.Response.WriteAsJsonAsync(
                new { title = "Validation Error", errors },
                cancellationToken);

            return true;
        }

        var (statusCode, title, detail) = exception switch
        {
            ArgumentException => ((int)HttpStatusCode.BadRequest, "Bad Request", exception.Message),
            KeyNotFoundException => ((int)HttpStatusCode.NotFound, "Not Found", exception.Message),
            InvalidOperationException => ((int)HttpStatusCode.UnprocessableEntity, "Invalid Operation", exception.Message),
            _ => ((int)HttpStatusCode.InternalServerError, "Server Error", "Terjadi kesalahan internal pada server.")
        };

        if (statusCode >= (int)HttpStatusCode.InternalServerError)
        {
            logger.LogError(exception, "Terjadi kesalahan: {Message}", exception.Message);
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            new { title, detail },
            cancellationToken);

        return true;
    }
}