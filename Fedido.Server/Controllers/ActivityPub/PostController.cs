using Fedido.Server.Interfaces;
using Microsoft.AspNetCore.Mvc;

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

    // [HttpGet("{postId:guid}")]
    // public async Task<ActionResult> GetPost(Guid postId)
    // {
    //     var postDefinitionBuilder = Builders<Post>.Filter;
    //     var postFilter = postDefinitionBuilder.Eq(i => i.Id, postId);
    //     
    //     var post = await _repository.GetSpecificItem(postFilter, "Posts", );
    //
    //     if (post.To.IsPublic)
    //     {
    //         
    //     }
    // }
}