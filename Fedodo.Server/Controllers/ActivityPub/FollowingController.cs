using Fedodo.Server.Interfaces;
using Fedodo.Server.Model.ActivityPub;
using Microsoft.AspNetCore.Mvc;

namespace Fedodo.Server.Controllers.ActivityPub;

[Route("Following")]
public class FollowingController : ControllerBase
{
    private readonly ILogger<FollowingController> _logger;
    private readonly IMongoDbRepository _repository;

    public FollowingController(ILogger<FollowingController> logger, IMongoDbRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [HttpGet]
    [Route("{userId:guid}")]
    public async Task<ActionResult<OrderedCollection<Activity>>> GetFollowings(Guid userId)
    {
        _logger.LogTrace($"Entered {nameof(GetFollowings)} in {nameof(FollowingController)}");

        var followings =
            await _repository.GetAll<Activity>(DatabaseLocations.OutboxFollow.Database,
                DatabaseLocations.OutboxFollow.Collection);

        var orderedCollection = new OrderedCollection<Activity>
        {
            Summary = $"Followings of {userId}",
            OrderedItems = followings
        };

        return Ok(orderedCollection);
    }
}