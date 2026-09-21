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
        try
        {
            var result = posService.Checkout(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }
}