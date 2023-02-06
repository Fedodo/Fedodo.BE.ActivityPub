using Fedodo.Server.Interfaces;
using Fedodo.Server.Model.ActivityPub;
using Fedodo.Server.Model.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace Fedodo.Server.Controllers.ActivityPub;

[Route("Likes")]
public class LikesController : ControllerBase
{
    private readonly ILogger<SharesController> _logger;
    private readonly IMongoDbRepository _repository;

    public LikesController(ILogger<SharesController> logger, IMongoDbRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [HttpGet]
    [Route("{postId}")]
    public async Task<ActionResult<OrderedCollection<string>>> GetLikes(Uri postId)
    {
        _logger.LogTrace($"Entered {nameof(GetLikes)} in {nameof(LikesController)}");

        var likes = await _repository.GetAll<LikeHelper>(DatabaseLocations.Likes.Database, postId.ToString());

        var orderedCollection = new OrderedCollection<string>
        {
            Summary = $"Likes of Post with id: {postId}",
            OrderedItems = likes.Select(i => i.Like.ToString())
        };

        return Ok(orderedCollection);
    }
}