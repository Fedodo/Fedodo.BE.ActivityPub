using Microsoft.AspNetCore.Mvc;

namespace ActivityPubServer.Controllers;

[Route("HealthCheck")]
public class HealthCheckController : ControllerBase
{
    [HttpGet]
    public ActionResult CheckHealth()
    {
        return Ok();
    }
}