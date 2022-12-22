using System.Text;
using ActivityPubServer.Model.ActivityPub;
using Microsoft.AspNetCore.Mvc;

namespace ActivityPubServer.Controllers;

[Route("Inbox")]
public class InboxController : ControllerBase
{
    private readonly ILogger<InboxController> _logger;

    public InboxController(ILogger<InboxController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult> Log(object asdf)
    {
        var bodyStr = "";
        var req = HttpContext.Request;
        
        bodyStr = Encoding.UTF8.GetString((await HttpContext.Request.BodyReader.ReadAsync()).Buffer);
        
        _logger.LogDebug(bodyStr);
        
        return Ok();
    }

    [HttpPost("{id}")]
    public async Task<ActionResult> Log(Guid id)
    {
        var bodyStr = "";
        var req = HttpContext.Request;

        bodyStr = Encoding.UTF8.GetString((await HttpContext.Request.BodyReader.ReadAsync()).Buffer);

        _logger.LogDebug(bodyStr);

        return Ok();
    }
}