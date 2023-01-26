using Fedodo.Server.Interfaces;
using Fedodo.Server.Model.ActivityPub;
using Fedodo.Server.Model.Helpers;
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
    public async Task<ActionResult<OrderedCollection<string>>> GetFollowings(Guid userId)
    {
        _logger.LogTrace($"Entered {nameof(GetFollowings)} in {nameof(FollowingController)}");

        var followings =
            await _repository.GetAll<FollowingHelper>(DatabaseLocations.Followings.Database, userId.ToString());

        var orderedCollection = new OrderedCollection<string>
        {
            Summary = $"Followings of {userId}",
            OrderedItems = followings.Select(i => i.Following.ToString())
        };

        return Ok(orderedCollection);
    }
}