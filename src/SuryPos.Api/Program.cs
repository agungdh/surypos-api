using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;
using SuryPos.Api.Exceptions;
using SuryPos.Data.Repositories;
using SuryPos.Domain.Interfaces;
using SuryPos.Service.Services;
using SuryPos.Service.Validators;

var builder = WebApplication.CreateBuilder(args);

// 1. Register Controllers & OpenAPI/Scalar
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => ValidationErrorKeys.Normalize(kvp.Key),
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

            return new BadRequestObjectResult(new { title = "Validation Error", errors });
        };
    });
builder.Services.AddOpenApi();

// 2. Register Dependency Injection (DI) - Repositories & Services
builder.Services.AddSingleton<IProductRepository, ProductRepository>();
builder.Services.AddSingleton<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IPosService, PosService>();

// 3. Register FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CheckoutRequestValidator>();

// 4. Register Global Exception Handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// 5. Configure HTTP Request Pipeline
// Disarankan UseExceptionHandler dipasang di paling atas middleware pipeline
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthorization();
app.MapControllers();

app.Run();