using ActivityPubServer.Interfaces;
using ActivityPubServer.Model.ActivityPub;
using ActivityPubServer.Model.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace ActivityPubServer.Controllers;

[Route("Followers")]
public class FollowersController : ControllerBase
{
    private readonly ILogger<FollowersController> _logger;
    private readonly IMongoDbRepository _repository;

    public FollowersController(ILogger<FollowersController> logger, IMongoDbRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [HttpGet]
    [Route("{userId:guid}")]
    public async Task<ActionResult<OrderedCollection<string>>> GetFollowers(Guid userId)
    {
        _logger.LogTrace($"Entered {nameof(GetFollowers)} in {nameof(FollowersController)}");

        var followers = await _repository.GetAll<FollowingHelper>("Followers", userId.ToString());

        var orderedCollection = new OrderedCollection<string>
        {
            Summary = $"Followers of {userId}",
            OrderedItems = followers.Select(i => i.Following.ToString())
        };

        return Ok(orderedCollection);
    }
}