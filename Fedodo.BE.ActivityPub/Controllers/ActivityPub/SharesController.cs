using System.Web;
using CommonExtensions;
using Fedodo.BE.ActivityPub.Model.ActivityPub;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Fedodo.BE.ActivityPub.Controllers.ActivityPub;

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
    public async Task<ActionResult<OrderedCollectionPage<Activity>>> GetSharesPage(string postIdUrlEncoded,
        [FromQuery] int? page = null)
    {
        _logger.LogTrace($"Entered {nameof(GetSharesPage)} in {nameof(SharesController)}");

        if (page.IsNull()) return Ok(await GetSharesSummary(postIdUrlEncoded));

        var postId = HttpUtility.UrlDecode(postIdUrlEncoded);

        var builder = Builders<Activity>.Sort;
        var sort = builder.Descending(i => i.Published);

        var filterBuilder = new FilterDefinitionBuilder<Activity>();
        var filter = filterBuilder.Where(i => (string)i.Object == postId);

        var sharesOutbox = (await _repository.GetSpecificPaged(DatabaseLocations.OutboxAnnounce.Database,
            DatabaseLocations.OutboxAnnounce.Collection, (int)page, 20, sort, filter)).ToList();
        var sharesInbox = (await _repository.GetSpecificPaged(DatabaseLocations.InboxAnnounce.Database,
            DatabaseLocations.InboxAnnounce.Collection, (int)page, 20, sort, filter)).ToList();
        var shares = new List<Activity>();
        shares.AddRange(sharesOutbox);
        shares.AddRange(sharesInbox);
        shares.OrderByDescending(i => i.Published);
        var count = 0;
        if (shares.Count < 20) count = shares.Count;
        shares = shares.GetRange(0, count);

        var orderedCollection = new OrderedCollectionPage<Activity>
        {
            OrderedItems = shares,
            Id = new Uri(
                $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/shares/{HttpUtility.UrlEncode(postId)}/?page={page}"),
            Next = new Uri(
                $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/shares/{HttpUtility.UrlEncode(postId)}/?page={page + 1}"), // TODO
            Prev = new Uri(
                $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/shares/{HttpUtility.UrlEncode(postId)}/?page={page - 1}"), // TODO
            PartOf = new Uri(
                $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/shares/{HttpUtility.UrlEncode(postId)}")
        };

        return Ok(orderedCollection);
    }

    private async Task<OrderedPagedCollection> GetSharesSummary(string postIdUrlEncoded)
    {
        _logger.LogTrace($"Entered {nameof(GetSharesSummary)} in {nameof(SharesController)}");

        var postId = HttpUtility.UrlDecode(postIdUrlEncoded);

        var filterBuilder = new FilterDefinitionBuilder<Activity>();
        var filter = filterBuilder.Where(i => (string)i.Object == postId);

        var postCount = await _repository.CountSpecific(DatabaseLocations.InboxAnnounce.Database,
            DatabaseLocations.InboxAnnounce.Collection, filter);
        postCount += await _repository.CountSpecific(DatabaseLocations.OutboxAnnounce.Database,
            DatabaseLocations.OutboxAnnounce.Collection, filter);

        var orderedCollection = new OrderedPagedCollection
        {
            Id = new Uri(
                $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/shares/{HttpUtility.UrlEncode(postId)}"),
            TotalItems = postCount,
            First = new Uri(
                $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/shares/{HttpUtility.UrlEncode(postId)}?page=0"),
            Last = new Uri(
                $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/shares/{HttpUtility.UrlEncode(postId)}?page={postCount / 20}")
        };

        return orderedCollection;
    }
}