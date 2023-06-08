using System.Web;
using CommonExtensions;
using Fedodo.BE.ActivityPub.Constants;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.ActivityPub.Model.JsonConverters.Model;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Object = Fedodo.NuGet.ActivityPub.Model.CoreTypes.Object;

namespace Fedodo.BE.ActivityPub.Controllers.ActivityPub;

[Route("Likes")]
[Produces("application/json")]
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

        var postId = HttpUtility.UrlDecode(postIdUrlEncoded);

        var filterBuilder = new FilterDefinitionBuilder<Activity>();
        var filter = filterBuilder.Where(i => i.Object!.StringLinks!.First() == postId);

        var postCount = await _repository.CountSpecific(DatabaseLocations.InboxLike.Database,
            DatabaseLocations.InboxLike.Collection, filter);
        postCount += await _repository.CountSpecific(DatabaseLocations.OutboxLike.Database,
            DatabaseLocations.OutboxLike.Collection, filter);

        var orderedCollection = new OrderedCollection
        {
            Id = new Uri(
                $"https://{GeneralConstants.DomainName}/likes/{HttpUtility.UrlEncode(postId)}"),
            First = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/likes/{HttpUtility.UrlEncode(postId)}?page=0"
                }
            },
            Last = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/likes/{HttpUtility.UrlEncode(postId)}?page={postCount / 20}"
                }
            },
            TotalItems = postCount
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

        var postId = HttpUtility.UrlDecode(postIdUrlEncoded);

        var builder = Builders<Activity>.Sort;
        var sort = builder.Descending(i => i.Published);

        var filterBuilder = new FilterDefinitionBuilder<Activity>();
        var filter = filterBuilder.Where(i => i.Object!.StringLinks!.First() == postId);

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

        var encodedPostId = HttpUtility.UrlEncode(postId);

        var orderedCollection = new OrderedCollectionPage
        {
            Items = new TripleSet<Object>
            {
                Objects = likes
            },
            Id = new Uri(
                $"https://{GeneralConstants.DomainName}/likes/{encodedPostId}/?page={page}"),
            Next = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/likes/{encodedPostId}/?page={page + 1}" // TODO
                }
            },
            Prev = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/likes/{encodedPostId}/?page={page - 1}" // TODO
                }
            },
            PartOf = new TripleSet<OrderedCollection>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/likes/{encodedPostId}"
                }
            },
            TotalItems = likes.Count
        };

        return Ok(orderedCollection);
    }
}