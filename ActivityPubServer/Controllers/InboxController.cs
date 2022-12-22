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

    [HttpGet]
    public ActionResult Log(string something)
    {
        _logger.LogDebug(something);
        
        return Ok();
    }
    
    [HttpGet("{id}")]
    public ActionResult Log(Guid id, string something)
    {
        _logger.LogDebug(something);
        
        return Ok();
    }
}