using Microsoft.AspNetCore.Mvc;

namespace ActivityPubServer.Controllers;

[Route("Posts")]
public class PostController : ControllerBase
{
    [HttpPost]
    public ActionResult Create()
    {
        
        
        return Ok();
    }
}