using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Yarp.Controllers;

[AllowAnonymous]
[ApiController]
[Route("[controller]")]
public class PingController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var res = new
        {
            app = "YARP API Gateway",
            category = "Core",
            version = "1.0.0",
            host = Environment.MachineName,
            time = DateTime.Now.ToShortTimeString(),
            remoteIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
        };

        return Ok(res);
    }
}