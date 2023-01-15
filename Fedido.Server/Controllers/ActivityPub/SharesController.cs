using Fedido.Server.Interfaces;
using Fedido.Server.Model.ActivityPub;
using Fedido.Server.Model.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace Fedido.Server.Controllers.ActivityPub;

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
    [Route("{postId:guid}")]
    public async Task<ActionResult<OrderedCollection<string>>> GetShares(Guid postId)
    {
        _logger.LogTrace($"Entered {nameof(GetShares)} in {nameof(SharesController)}");

        var shares = await _repository.GetAll<ShareHelper>("Shares", postId.ToString());

        var orderedCollection = new OrderedCollection<string>
        {
            Summary = $"Shares of {postId}",
            OrderedItems = shares.Select(i => i.Share.ToString())
        };

        return Ok(orderedCollection);
    }
}