using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using SuryPos.Api.Exceptions;
using SuryPos.Data;
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

// 2. Register Postgres (EF Core + snake_case naming)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
        .UseSnakeCaseNamingConvention());

// 3. Register Dependency Injection (DI) - Repositories & Services
// Scoped agar satu DbContext dipakai bersama dalam satu request/transaksi.
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<IPosService, PosService>();

// 4. Register FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CheckoutRequestValidator>();

// 5. Register Global Exception Handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// 6. Migrasi / validasi database saat startup.
// Database:AutoMigrate=true  -> jalankan Migrate() otomatis (default di Development).
// Database:AutoMigrate=false -> validate-only, fail fast kalau ada migrasi pending.
var autoMigrate = builder.Configuration.GetValue("Database:AutoMigrate", true);
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (autoMigrate)
    {
        await db.Database.MigrateAsync();
    }
    else
    {
        var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
        if (pending.Count > 0)
            throw new InvalidOperationException(
                $"Ada migrasi database yang belum diterapkan: {string.Join(", ", pending)}. " +
                "Jalankan 'dotnet ef database update' atau set Database:AutoMigrate=true.");
    }
}

// 7. Configure HTTP Request Pipeline
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

public partial class Program { }
