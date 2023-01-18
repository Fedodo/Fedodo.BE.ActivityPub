using CommonExtensions;
using Fedido.Server.Interfaces;
using Fedido.Server.Model;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Fedido.Server.Controllers;

[Route(".well-known")]
public class WellKnownController : ControllerBase
{
    private readonly ILogger<Webfinger> _logger;
    private readonly IMongoDbRepository _repository;

    public WellKnownController(ILogger<Webfinger> logger, IMongoDbRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [HttpGet]
    [Route("webfinger")]
    public async Task<ActionResult<Webfinger>> GetWebfinger(string resource)
    {
        _logger.LogTrace(
            $"Entered {nameof(GetWebfinger)} in {nameof(WellKnownController)} with {nameof(resource)} = {resource}");

        var filterDefinitionBuilder = Builders<Webfinger>.Filter;
        var filter = filterDefinitionBuilder.Eq(i => i.Subject, resource);

        var finger = await _repository.GetSpecificItem(filter, DatabaseLocations.Webfinger.Database,
            DatabaseLocations.Webfinger.Collection);

        if (finger.IsNull())
        {
            _logger.LogWarning($"{nameof(finger)} is null");
            return BadRequest("Not found WebFinger.");
        }

        return Ok(finger);
    }
}