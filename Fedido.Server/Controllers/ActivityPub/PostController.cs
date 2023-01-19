using Fedido.Server.Extensions;
using Fedido.Server.Interfaces;
using Fedido.Server.Model.ActivityPub;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Fedido.Server.Controllers.ActivityPub;

[Route("Posts")]
public class PostController : ControllerBase
{
    private readonly ILogger<PostController> _logger;
    private readonly IMongoDbRepository _repository;

    public PostController(ILogger<PostController> logger, IMongoDbRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [HttpGet("{postId:guid}")]
    public async Task<ActionResult<Post>> GetPost(Guid postId)
    {
        var postDefinitionBuilder = Builders<Post>.Filter;
        var postFilter = postDefinitionBuilder.Eq(i => i.Id,
            new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/posts/{postId}"));

        var post = await _repository.GetSpecificItem(postFilter, DatabaseLocations.OutboxNotes.Database,
            DatabaseLocations.OutboxNotes.Collection);

        if (post.IsPostPublic())
            return Ok(post);
        return Forbid("Not a public post");
    }
}