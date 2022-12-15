using ActivityPubServer.Model;
using Microsoft.AspNetCore.Mvc;

namespace ActivityPubServer.Controllers;

[Route("/.well-known/webfinger")]
public class WebfingerController : ControllerBase
{
    [HttpGet]
    public ActionResult<Webfinger> GetWebfinger()
    {
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