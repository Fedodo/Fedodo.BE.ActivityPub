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
    public ActionResult Log(string something)
    {
        _logger.LogDebug(something);
        
        return Ok();
    }
    
    [HttpPost("{id}")]
    public ActionResult Log(Guid id, string something)
    {
        _logger.LogDebug(something);
        
        return Ok();
    }
}