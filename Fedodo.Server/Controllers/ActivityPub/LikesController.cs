using System.Web;
using CommonExtensions;
using Fedodo.Server.Interfaces;
using Fedodo.Server.Model.ActivityPub;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

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

    private async Task<OrderedPagedCollection> GetLikes(string postIdUrlEncoded)
    {
        _logger.LogTrace($"Entered {nameof(GetLikes)} in {nameof(LikesController)}");

        var postId = HttpUtility.UrlDecode(postIdUrlEncoded);

        var filterBuilder = new FilterDefinitionBuilder<Activity>();
        var filter = filterBuilder.Where(i => (string)i.Object == postId);

        var postCount = await _repository.CountSpecific(DatabaseLocations.InboxAnnounce.Database,
            DatabaseLocations.InboxAnnounce.Collection, filter);
        postCount += await _repository.CountSpecific(DatabaseLocations.OutboxAnnounce.Database,
            DatabaseLocations.OutboxAnnounce.Collection, filter);

        var orderedCollection = new OrderedPagedCollection
        {
            Id = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/likes/{postId}"),
            TotalItems = postCount,
            First = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/likes/{postId}?page=0"),
            Last = new Uri(
                $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/likes/{postId}?page={postCount / 20}")
        };

        return orderedCollection;
    }

    [HttpGet]
    [Route("{postIdUrlEncoded}")]
    public async Task<ActionResult<OrderedCollectionPage<Activity>>> GetLikesPage(string postIdUrlEncoded,
        [FromQuery] int? page = null)
    {
        _logger.LogTrace($"Entered {nameof(GetLikesPage)} in {nameof(LikesController)}");

        if (page.IsNull()) return Ok(await GetLikes(postIdUrlEncoded));

        var postId = HttpUtility.UrlDecode(postIdUrlEncoded);

        var builder = Builders<Activity>.Sort;
        var sort = builder.Descending(i => i.Published);

        var filterBuilder = new FilterDefinitionBuilder<Activity>();
        var filter = filterBuilder.Where(i => (string)i.Object == postId);

        var likesOutbox = (await _repository.GetSpecificPaged(DatabaseLocations.OutboxLike.Database,
            DatabaseLocations.OutboxLike.Collection, (int)page, 20, sort, filter)).ToList();
        var likesInbox = (await _repository.GetSpecificPaged(DatabaseLocations.InboxLike.Database,
            DatabaseLocations.InboxLike.Collection, (int)page, 20, sort, filter)).ToList();
        var likes = new List<Activity>();
        likes.AddRange(likesOutbox);
        likes.AddRange(likesInbox);
        likes.OrderByDescending(i => i.Published);
        var count = 0;
        if (likes.Count < 20) count = likes.Count;
        likes = likes.GetRange(0, count);

        var orderedCollection = new OrderedCollectionPage<Activity>
        {
            OrderedItems = likes,
            Id = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/likes/{postId}/?page={page}"),
            Next = new Uri(
                $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/likes/{postId}/?page={page + 1}"), // TODO
            Prev = new Uri(
                $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/likes/{postId}/?page={page - 1}"), // TODO
            PartOf = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/likes/{postId}")
        };

        return Ok(orderedCollection);
    }
}