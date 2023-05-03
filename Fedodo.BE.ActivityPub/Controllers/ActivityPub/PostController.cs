using Fedodo.BE.ActivityPub.Extensions;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.ActivityPub.Model.ObjectTypes;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Fedodo.BE.ActivityPub.Controllers.ActivityPub;

[Route("Activities")]
[Produces("application/json")]
public class PostController : ControllerBase
{
    private readonly ILogger<PostController> _logger;
    private readonly IMongoDbRepository _repository;

    public PostController(ILogger<PostController> logger, IMongoDbRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [HttpGet("{activityId:guid}")]
    public async Task<ActionResult<Note>> GetPost(Guid activityId)
    {
        var postDefinitionBuilder = Builders<Activity>.Filter;
        var postFilter = postDefinitionBuilder.Eq(i => i.Id,
            new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/activities/{activityId}"));

        var post = await _repository.GetSpecificItem(postFilter, DatabaseLocations.OutboxCreate.Database,
            DatabaseLocations.OutboxCreate.Collection);

        if (post.IsActivityPublic())
            return Ok(post);

        return Forbid("Not a public post");
    }
}