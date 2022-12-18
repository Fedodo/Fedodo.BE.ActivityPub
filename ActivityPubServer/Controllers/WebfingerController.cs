using ActivityPubServer.Interfaces;
using ActivityPubServer.Model;
using CommonExtensions;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace ActivityPubServer.Controllers;

[Route("/.well-known/webfinger")]
public class WebfingerController : ControllerBase
{
    private readonly ILogger<Webfinger> _logger;
    private readonly IMongoDbRepository _repository;

    public WebfingerController(ILogger<Webfinger> logger, IMongoDbRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<Webfinger>> GetWebfinger(string resource)
    {
        _logger.LogTrace(
            $"Entered {nameof(GetWebfinger)} in {nameof(WebfingerController)} with {nameof(resource)} = {resource}");

        var filterDefinitionBuilder = Builders<Webfinger>.Filter;
        var filter = filterDefinitionBuilder.Eq(i => i.Subject, resource);

        var finger = await _repository.GetSpecific(filter, "ActivityPub", "Webfingers");

        if (finger.IsNull())
        {
            _logger.LogWarning($"{nameof(finger)} is null");
            return BadRequest("Not found WebFinger.");
        }

        return Ok(finger);
    }
}