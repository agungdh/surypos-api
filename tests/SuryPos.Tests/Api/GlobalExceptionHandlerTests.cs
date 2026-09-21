using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SuryPos.Api.Exceptions;

namespace SuryPos.Tests.Api;

public class GlobalExceptionHandlerTests
{
    private sealed class CapturingLogger : ILogger<GlobalExceptionHandler>
    {
        public List<LogLevel> Levels { get; } = [];
        IDisposable? ILogger.BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) => Levels.Add(logLevel);
    }

    private static (GlobalExceptionHandler Handler, CapturingLogger Logger, DefaultHttpContext Context)
        CreateHandler()
    {
        var logger = new CapturingLogger();
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return (new GlobalExceptionHandler(logger), logger, context);
    }

    private static async Task<JsonDocument> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return await JsonDocument.ParseAsync(context.Response.Body);
    }

    [Fact]
    public async Task ValidationException_Returns422PerField_WithoutLogging()
    {
        var (handler, logger, context) = CreateHandler();
        var exception = new ValidationException(
        [
            new ValidationFailure("Items", "Daftar item belanja wajib diisi.")
        ]);

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);
        var body = await ReadBodyAsync(context);

        Assert.True(handled);
        Assert.Equal(422, context.Response.StatusCode);
        Assert.Equal("Validation Error", body.RootElement.GetProperty("title").GetString());
        var errors = body.RootElement.GetProperty("errors");
        Assert.Contains("Daftar item belanja wajib diisi.",
            errors.GetProperty("items").EnumerateArray().Select(e => e.GetString()));
        Assert.False(body.RootElement.TryGetProperty("status", out _));
        Assert.False(body.RootElement.TryGetProperty("instance", out _));
        Assert.Empty(logger.Levels);
    }

    [Fact]
    public async Task UnexpectedException_Returns500GenericMessage_WithLogging()
    {
        var (handler, logger, context) = CreateHandler();

        var handled = await handler.TryHandleAsync(
            context, new InvalidOperationException("rahasia dapur"), CancellationToken.None);
        var body = await ReadBodyAsync(context);

        Assert.True(handled);
        Assert.Equal(500, context.Response.StatusCode);
        Assert.Equal("Server Error", body.RootElement.GetProperty("title").GetString());
        Assert.DoesNotContain("rahasia dapur", body.RootElement.GetRawText());
        Assert.Equal([LogLevel.Error], logger.Levels);
    }

    [Fact]
    public async Task KeyNotFoundException_IsServerError_Not404()
    {
        var (handler, logger, context) = CreateHandler();

        await handler.TryHandleAsync(
            context, new KeyNotFoundException("tidak ada"), CancellationToken.None);

        Assert.Equal(500, context.Response.StatusCode);
        Assert.Equal([LogLevel.Error], logger.Levels);
    }
}
