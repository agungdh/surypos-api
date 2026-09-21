using Scalar.AspNetCore;
using SuryPos.Data.Repositories;
using SuryPos.Domain.Interfaces;
using SuryPos.Service.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Register Controllers & OpenAPI/Swagger
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// 2. Register Dependency Injection (DI)
// Registrasi Repository sebagai Singleton agar data In-Memory tidak reset tiap request HTTP
builder.Services.AddSingleton<IProductRepository, ProductRepository>();
builder.Services.AddSingleton<ITransactionRepository, TransactionRepository>();

// Registrasi Service sebagai Scoped (standar best practice untuk business logic)
builder.Services.AddScoped<IPosService, PosService>();

var app = builder.Build();

// 3. Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthorization();
app.MapControllers();

app.Run();