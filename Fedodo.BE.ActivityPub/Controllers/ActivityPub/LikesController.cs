using System.Web;
using CommonExtensions;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.ActivityPub.Model.JsonConverters.Model;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Fedodo.BE.ActivityPub.Controllers.ActivityPub;

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

    private async Task<OrderedCollection> GetLikes(string postIdUrlEncoded)
    {
        _logger.LogTrace($"Entered {nameof(GetLikes)} in {nameof(LikesController)}");

        var postId = new Uri(HttpUtility.UrlDecode(postIdUrlEncoded));

        var filterBuilder = new FilterDefinitionBuilder<Activity>();
        var filter = filterBuilder.Where(i => i.Object.Id == postId);

        var postCount = await _repository.CountSpecific(DatabaseLocations.InboxLike.Database,
            DatabaseLocations.InboxLike.Collection, filter);
        postCount += await _repository.CountSpecific(DatabaseLocations.OutboxLike.Database,
            DatabaseLocations.OutboxLike.Collection, filter);

        var orderedCollection = new OrderedCollection()
        {
            Id = new Uri(
                $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/likes/{HttpUtility.UrlEncode(postId.ToString())}"),
            First = new()
            {
                StringLinks = new[]
                {
                    new Uri(
                        $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/likes/{HttpUtility.UrlEncode(postId.ToString())}?page=0"),
                }
            },
            Last = new TripleSet<OrderedCollectionPage>()
            {
                StringLinks = new[]
                {
                    new Uri(
                        $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/likes/{HttpUtility.UrlEncode(postId.ToString())}?page={postCount / 20}")
                }
            }
        };

        return orderedCollection;
    }

    [HttpGet]
    [Route("{postIdUrlEncoded}")]
    public async Task<ActionResult<OrderedCollectionPage>> GetLikesPage(string postIdUrlEncoded,
        [FromQuery] int? page = null)
    {
        _logger.LogTrace($"Entered {nameof(GetLikesPage)} in {nameof(LikesController)}");

        if (page.IsNull()) return Ok(await GetLikes(postIdUrlEncoded));

        var postId = new Uri(HttpUtility.UrlDecode(postIdUrlEncoded));

        var builder = Builders<Activity>.Sort;
        var sort = builder.Descending(i => i.Published);

        var filterBuilder = new FilterDefinitionBuilder<Activity>();
        var filter = filterBuilder.Where(i => i.Object.Id == postId);

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

        var encodedPostId = HttpUtility.UrlEncode(postId.ToString());

        var orderedCollection = new OrderedCollectionPage
        {
            Id = new Uri(
                $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/likes/{encodedPostId}/?page={page}"),
            Next = new TripleSet<OrderedCollectionPage>()
            {
                StringLinks = new[]
                {
                    new Uri(
                        $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/likes/{encodedPostId}/?page={page + 1}"), // TODO
                }
            },
            Prev = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    new Uri(
                        $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/likes/{encodedPostId}/?page={page - 1}"
                    ) // TODO
                }
            },
            PartOf = new TripleSet<OrderedCollection>()
            {
                StringLinks = new []
                {
                    new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/likes/{encodedPostId}")
                }
            }
        };

        return Ok(orderedCollection);
    }
}