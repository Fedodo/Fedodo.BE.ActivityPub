using ActivityPubServer.Model;
using Microsoft.AspNetCore.Mvc;

namespace ActivityPubServer.Controllers;

[Route("/.well-known/webfinger")]
public class WebfingerController : ControllerBase
{
    private readonly ILogger<Webfinger> _logger;

    public WebfingerController(ILogger<Webfinger> logger)
    {
        _logger = logger;
    }
    
    [HttpGet]
    public ActionResult<Webfinger> GetWebfinger()
    {
        _logger.LogTrace($"Entered {nameof(GetWebfinger)}");
        
        Webfinger finger = new()
        {
            Subject = new Uri("acct:Lukas@ap.lna-dev.net"),
            Links = new []
            {
                new Link()
                {
                    Rel = "self",
                    Type = "application/activity+json",
                    Href = new Uri("https://ap.lna-dev.net/actor")
                }
            }
        };

        return Ok(finger);
    }
}