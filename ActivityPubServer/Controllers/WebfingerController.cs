using ActivityPubServer.Interfaces;
using ActivityPubServer.Model;
using CommonExtensions;
using Microsoft.AspNetCore.Mvc;

namespace ActivityPubServer.Controllers;

[Route("/.well-known/webfinger")]
public class WebfingerController : ControllerBase
{
    private readonly ILogger<Webfinger> _logger;
    private readonly IInMemRepository _inMemRepository;

    public WebfingerController(ILogger<Webfinger> logger, IInMemRepository inMemRepository)
    {
        _logger = logger;
        _inMemRepository = inMemRepository;
    }
    
    [HttpGet]
    public ActionResult<Webfinger> GetWebfinger(string resource)
    {
        _logger.LogTrace($"Entered {nameof(GetWebfinger)} in {nameof(WebfingerController)} with {nameof(resource)} = {resource}");

        var finger = _inMemRepository.GetWebfinger(resource);

        if (finger.IsNull())
        {
            return BadRequest("Not found WebFinger.");
        }
        
        return Ok(finger);
    }
}