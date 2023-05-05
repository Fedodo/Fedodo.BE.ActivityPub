using CommonExtensions;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.ActivityPub.Model.JsonConverters.Model;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Object = Fedodo.NuGet.ActivityPub.Model.CoreTypes.Object;

namespace Fedodo.BE.ActivityPub.Controllers.ActivityPub;

[Route("Following")]
[Produces("application/json")]
public class FollowingController : ControllerBase
{
    private readonly ILogger<FollowingController> _logger;
    private readonly IMongoDbRepository _repository;

    public FollowingController(ILogger<FollowingController> logger, IMongoDbRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    private async Task<OrderedCollection> GetFollowings(Guid userId)
    {
        _logger.LogTrace($"Entered {nameof(GetFollowings)} in {nameof(FollowingController)}");

        var fullUserId = $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/actor/{userId}";

        var filterBuilder = new FilterDefinitionBuilder<Activity>();
        var filter = filterBuilder.Where(i =>
            i.Actor != null && i.Actor.StringLinks != null && i.Actor.StringLinks.ToList()[0].ToString() == fullUserId);

        var postCount = await _repository.CountSpecific(DatabaseLocations.OutboxFollow.Database,
            DatabaseLocations.OutboxFollow.Collection, filter);

        var orderedCollection = new OrderedCollection
        {
            Id = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/following/{userId}"),
            First = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/following/{userId}?page=0"
                }
            },
            Last = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/following/{userId}?page={postCount / 20}"
                }
            },
            TotalItems = postCount
        };

        return orderedCollection;
    }

    [HttpGet]
    [Route("{userId}")]
    public async Task<ActionResult<OrderedCollectionPage>> GetFollowingsPage(Guid userId,
        [FromQuery] int? page = null)
    {
        _logger.LogTrace($"Entered {nameof(GetFollowingsPage)} in {nameof(FollowingController)}");

        var fullUserId = $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/actor/{userId}";

        if (page.IsNull()) return Ok(await GetFollowings(userId));

        var builder = Builders<Activity>.Sort;
        var sort = builder.Descending(i => i.Published);

        var filterBuilder = new FilterDefinitionBuilder<Activity>();
        var filter = filterBuilder.Where(i =>
            i.Actor != null && i.Actor.StringLinks != null && i.Actor.StringLinks.ToList()[0].ToString() == fullUserId);

        var followings = (await _repository.GetSpecificPaged(DatabaseLocations.OutboxFollow.Database,
            DatabaseLocations.OutboxFollow.Collection, (int)page, 20, sort, filter)).ToList();

        var orderedCollection = new OrderedCollectionPage
        {
            Items = new TripleSet<Object>
            {
                Objects = followings
            },
            Id = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/following/{userId}/?page={page}"),
            Next = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/following/{userId}/?page={page + 1}" // TODO
                }
            },
            Prev = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/following/{userId}/?page={page - 1}" // TODO
                }
            },
            PartOf = new TripleSet<OrderedCollection>
            {
                StringLinks = new[]
                {
                    $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/following/{userId}" // TODO
                }
            }
        };

        return Ok(orderedCollection);
    }
}