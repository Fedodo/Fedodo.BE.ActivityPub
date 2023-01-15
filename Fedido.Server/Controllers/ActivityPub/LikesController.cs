using Fedido.Server.Interfaces;
using Fedido.Server.Model.ActivityPub;
using Fedido.Server.Model.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace Fedido.Server.Controllers.ActivityPub;

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
    [Route("{postId:guid}")]
    public async Task<ActionResult<OrderedCollection<string>>> GetLikes(Guid postId)
    {
        _logger.LogTrace($"Entered {nameof(GetLikes)} in {nameof(LikesController)}");

        var shares = await _repository.GetAll<LikeHelper>("Likes", postId.ToString());

        var orderedCollection = new OrderedCollection<string>
        {
            Summary = $"Likes of {postId}",
            OrderedItems = shares.Select(i => i.Like.ToString())
        };

        return Ok(orderedCollection);
    }
}