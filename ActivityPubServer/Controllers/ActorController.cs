using ActivityPubServer.Interfaces;
using ActivityPubServer.Model;
using Microsoft.AspNetCore.Mvc;

namespace ActivityPubServer.Controllers;

[Route("Actor")]
public class ActorController : ControllerBase
{
    private readonly ILogger<ActorController> _logger;
    private readonly IInMemRepository _repository;

    public ActorController(ILogger<ActorController> logger, IInMemRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }
    
    [HttpGet]
    public ActionResult<Actor> GetActor()
    {
        _logger.LogTrace($"Entered {nameof(GetActor)} in {nameof(ActorController)}");

        var actor = _repository.GetActor();
        
        return Ok(actor);
    }
}