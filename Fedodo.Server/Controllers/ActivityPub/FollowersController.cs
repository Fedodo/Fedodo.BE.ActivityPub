using Fedodo.Server.Interfaces;
using Fedodo.Server.Model.ActivityPub;
using Fedodo.Server.Model.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace Fedodo.Server.Controllers.ActivityPub;

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

        var followers =
            await _repository.GetAll<FollowingHelper>(DatabaseLocations.InboxFollow.Database,
                DatabaseLocations.InboxFollow.Collection);

        var orderedCollection = new OrderedCollection<string>
        {
            Summary = $"Followers of {userId}",
            OrderedItems = followers.Select(i => i.Following.ToString())
        };

        return Ok(orderedCollection);
    }
}