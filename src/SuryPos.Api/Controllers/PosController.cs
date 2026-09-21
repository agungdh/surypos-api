using Microsoft.AspNetCore.Mvc;
using SuryPos.Service.DTOs;
using SuryPos.Service.Services;

namespace SuryPos.Api.Controllers;

[ApiController]
[Route("pos")]
public class PosController(IPosService posService) : ControllerBase
{
    [HttpGet("products")]
    public async Task<IActionResult> GetProducts(CancellationToken ct)
    {
        var products = await posService.GetProductsAsync(ct);
        return Ok(products);
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequestDto request, CancellationToken ct)
    {
        // Tanpa try-catch!
        // Kalau terjadi error, biarkan exception meluncur ke GlobalExceptionHandler
        var result = await posService.CheckoutAsync(request, ct);
        return Ok(result);
    }
}
