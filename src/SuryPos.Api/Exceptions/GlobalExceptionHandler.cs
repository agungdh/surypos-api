using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SuryPos.Api.Exceptions;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Terjadi kesalahan: {Message}", exception.Message);

        var (statusCode, title, detail) = exception switch
        {
            ArgumentException => ((int)HttpStatusCode.BadRequest, "Bad Request", exception.Message),
            ValidationException validationEx => (
                (int)HttpStatusCode.UnprocessableEntity, 
                "Validation Error", 
                string.Join("; ", validationEx.Errors.Select(e => e.ErrorMessage))
            ),
            KeyNotFoundException => ((int)HttpStatusCode.NotFound, "Not Found", exception.Message),
            InvalidOperationException => ((int)HttpStatusCode.UnprocessableEntity, "Invalid Operation", exception.Message),
            _ => ((int)HttpStatusCode.InternalServerError, "Server Error", "Terjadi kesalahan internal pada server.")
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}