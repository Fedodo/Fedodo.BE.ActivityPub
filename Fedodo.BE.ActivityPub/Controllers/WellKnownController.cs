using CommonExtensions;
using Fedodo.BE.ActivityPub.Model;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Fedodo.BE.ActivityPub.Controllers;

[Route(".well-known")]
[Produces("application/json")]
public class WellKnownController : ControllerBase
{
    private readonly ILogger<WellKnownController> _logger;
    private readonly IMongoDbRepository _repository;

    public WellKnownController(ILogger<WellKnownController> logger, IMongoDbRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [HttpGet]
    [Route("webfinger")]
    public async Task<ActionResult<Webfinger>> GetWebfingerAsync(string resource)
    {
        _logger.LogTrace(
            $"Entered {nameof(GetWebfingerAsync)} in {nameof(WellKnownController)} with {nameof(resource)} = {resource}");

        var filterDefinitionBuilder = Builders<Webfinger>.Filter;
        var filter = filterDefinitionBuilder.Eq(i => i.Subject, resource);

        var finger = await _repository.GetSpecificItem(filter, DatabaseLocations.Webfinger.Database,
            DatabaseLocations.Webfinger.Collection);

        if (finger.IsNotNull()) return Ok(finger);

        _logger.LogWarning($"{nameof(finger)} is null");
        return BadRequest("Not found WebFinger.");
    }
}