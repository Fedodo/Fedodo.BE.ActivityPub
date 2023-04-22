using System.Web;
using CommonExtensions;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.ActivityPub.Model.JsonConverters.Model;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Object = Fedodo.NuGet.ActivityPub.Model.CoreTypes.Object;

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
    public async Task<ActionResult<OrderedCollectionPage>> GetSharesPage(string postIdUrlEncoded,
        [FromQuery] int? page = null)
    {
        _logger.LogTrace($"Entered {nameof(GetSharesPage)} in {nameof(SharesController)}");

        if (page.IsNull()) return Ok(await GetSharesSummary(postIdUrlEncoded));

        var postId = new Uri(HttpUtility.UrlDecode(postIdUrlEncoded));

        var builder = Builders<Activity>.Sort;
        var sort = builder.Descending(i => i.Published);

        var filterBuilder = new FilterDefinitionBuilder<Activity>();
        var filter = filterBuilder.Where(i => i.Object.Objects.First().Id == postId);

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

        var orderedCollection = new OrderedCollectionPage
        {
            Items = new TripleSet<Object>()
            {
                Objects = shares
            },
            Id = new Uri(
                $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/shares/{HttpUtility.UrlEncode(postId.ToString())}/?page={page}"),
            Next = new()
            {
                StringLinks = new[]
                {
                    new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/shares/{HttpUtility.UrlEncode(postId.ToString())}/?page={page + 1}") // TODO
                }
            },
            Prev = new()
            {
                StringLinks = new[]
                {
                    new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/shares/{HttpUtility.UrlEncode(postId.ToString())}/?page={page - 1}") // TODO
                }
            },
            PartOf = new()
            {
                StringLinks = new[]
                {
                    new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/shares/{HttpUtility.UrlEncode(postId.ToString())}") // TODO
                }
            }
        };

        return Ok(orderedCollection);
    }

    private async Task<OrderedCollection> GetSharesSummary(string postIdUrlEncoded)
    {
        _logger.LogTrace($"Entered {nameof(GetSharesSummary)} in {nameof(SharesController)}");

        var postId = new Uri(HttpUtility.UrlDecode(postIdUrlEncoded));

        var filterBuilder = new FilterDefinitionBuilder<Activity>();
        var filter = filterBuilder.Where(i => i.Object.Objects.First().Id == postId);

        var postCount = await _repository.CountSpecific(DatabaseLocations.InboxAnnounce.Database,
            DatabaseLocations.InboxAnnounce.Collection, filter);
        postCount += await _repository.CountSpecific(DatabaseLocations.OutboxAnnounce.Database,
            DatabaseLocations.OutboxAnnounce.Collection, filter);

        var orderedCollection = new OrderedCollection()
        {
            Id = new Uri(
                $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/shares/{HttpUtility.UrlEncode(postId.ToString())}"),
            First = new()
            {
                StringLinks = new[]
                {
                    new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/shares/{HttpUtility.UrlEncode(postId.ToString())}?page=0") // TODO
                }
            },
            Last = new()
            {
                StringLinks = new[]
                {
                    new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/shares/{HttpUtility.UrlEncode(postId.ToString())}?page={postCount / 20}") // TODO
                }
            },
        };

        return orderedCollection;
    }
}