using Microsoft.AspNetCore.Mvc;
using SuryPos.Service.DTOs;
using SuryPos.Service.Services;

namespace SuryPos.Api.Controllers;

[ApiController]
[Route("pos")]
public class PosController(IPosService posService) : ControllerBase
{
    [HttpGet("products")]
    public IActionResult GetProducts()
    {
        var products = posService.GetProducts();
        return Ok(products);
    }

    [HttpPost("checkout")]
    public IActionResult Checkout([FromBody] CheckoutRequestDto request)
    {
        // Tanpa try-catch! 
        // Kalau terjadi error, biarkan exception meluncur ke GlobalExceptionHandler
        var result = posService.Checkout(request);
        return Ok(result);
    }
}