using System.Web;
using Fedodo.Server.Interfaces;
using Fedodo.Server.Model.ActivityPub;
using Fedodo.Server.Model.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace Fedodo.Server.Controllers.ActivityPub;

[Route("Likes")]
public class LikesController : ControllerBase
{
    private readonly ILogger<LikesController> _logger;
    private readonly IMongoDbRepository _repository;

    public LikesController(ILogger<LikesController> logger, IMongoDbRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [HttpGet]
    [Route("{postIdUrlEncoded}")]
    public async Task<ActionResult<OrderedCollection<string>>> GetLikes(string postIdUrlEncoded)
    {
        _logger.LogTrace($"Entered {nameof(GetLikes)} in {nameof(LikesController)}");

        var postId = HttpUtility.UrlDecode(postIdUrlEncoded);

        var likes = await _repository.GetAll<LikeHelper>(DatabaseLocations.Likes.Database, postId);

        var orderedCollection = new OrderedCollection<string>
        {
            Summary = $"Likes of Post with id: {postId}",
            OrderedItems = likes.Select(i => i.Like.ToString())
        };

        return Ok(orderedCollection);
    }
}