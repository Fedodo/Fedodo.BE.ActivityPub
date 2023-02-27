using Fedodo.Server.Extensions;
using Fedodo.Server.Interfaces;
using Fedodo.Server.Model.ActivityPub;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Fedodo.Server.Controllers.ActivityPub;

[Route("Activities")]
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
    public async Task<ActionResult<Post>> GetPost(Guid activityId)
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