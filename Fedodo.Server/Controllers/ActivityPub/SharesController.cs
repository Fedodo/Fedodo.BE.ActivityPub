using System.Web;
using Fedodo.Server.Interfaces;
using Fedodo.Server.Model.ActivityPub;
using Fedodo.Server.Model.Helpers;
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
    public async Task<ActionResult<OrderedCollection<string>>> GetShares(string postIdUrlEncoded)
    {
        _logger.LogTrace($"Entered {nameof(GetShares)} in {nameof(SharesController)}");
        
        var postId = HttpUtility.UrlDecode(postIdUrlEncoded);

        var shares = await _repository.GetAll<ShareHelper>(DatabaseLocations.Shares.Database, postId.ToString());

        var orderedCollection = new OrderedCollection<string>
        {
            Summary = $"Shares of Post: {postId}",
            OrderedItems = shares.Select(i => i.Share.ToString())
        };

        return Ok(orderedCollection);
    }
}