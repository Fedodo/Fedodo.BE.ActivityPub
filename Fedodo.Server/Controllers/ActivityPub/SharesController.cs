using System.Web;
using Fedodo.Server.Interfaces;
using Fedodo.Server.Model.ActivityPub;
using Microsoft.AspNetCore.Mvc;

namespace Fedodo.Server.Controllers.ActivityPub;

[Route("Shares")]
public class SharesController : ControllerBase
{
    private readonly ILogger<SharesController> _logger;
    private readonly IMongoDbRepository _repository;

    public SharesController(ILogger<SharesController> logger, IMongoDbRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [HttpGet]
    [Route("{postIdUrlEncoded}")]
    public async Task<ActionResult<OrderedCollection<Activity>>> GetShares(string postIdUrlEncoded)
    {
        _logger.LogTrace($"Entered {nameof(GetShares)} in {nameof(SharesController)}");

        var postId = HttpUtility.UrlDecode(postIdUrlEncoded);

        var shares = await _repository.GetAll<Activity>(DatabaseLocations.OutboxAnnounce.Database,
            DatabaseLocations.OutboxAnnounce.Collection);

        var orderedCollection = new OrderedCollection<Activity>
        {
            Summary = $"Shares of Post: {postId}",
            OrderedItems = shares
        };

        return Ok(orderedCollection);
    }
}