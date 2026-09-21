using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace SuryPos.Api.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? "unknown";
        return Ok(new { status = "healthy", version });
    }
}
