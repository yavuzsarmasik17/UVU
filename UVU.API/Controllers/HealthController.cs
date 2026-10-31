
using Microsoft.AspNetCore.Mvc;

namespace UVU.API.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "ok",
            application = "UVU.API",
            message = "UVU API çalışıyor"
        });
    }
}
