using CommonExtensions;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.ActivityPub.Model.JsonConverters.Model;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Object = Fedodo.NuGet.ActivityPub.Model.CoreTypes.Object;

namespace Fedodo.BE.ActivityPub.Controllers.ActivityPub;

[Route("Followers")]
[Produces("application/json")]
public class FollowersController : ControllerBase
{
    private readonly ILogger<FollowersController> _logger;
    private readonly IMongoDbRepository _repository;

    public FollowersController(ILogger<FollowersController> logger, IMongoDbRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    private async Task<OrderedCollection> GetFollowers(Guid userId)
    {
        _logger.LogTrace($"Entered {nameof(GetFollowers)} in {nameof(FollowersController)}");

        var fullUserId = $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/actor/{userId}";

        var filterBuilder = new FilterDefinitionBuilder<Activity>();
        var filter = filterBuilder.Where(i => i.Object!.StringLinks!.First() == fullUserId);

        var postCount = await _repository.CountSpecific(DatabaseLocations.InboxFollow.Database,
            DatabaseLocations.InboxFollow.Collection, filter);

        var orderedCollection = new OrderedCollection
        {
            Id = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/followers/{userId}"),
            First = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/followers/{userId}?page=0"
                }
            },
            Last = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/followers/{userId}?page={postCount / 20}"
                }
            },
            TotalItems = postCount
        };

        return orderedCollection;
    }

    [HttpGet]
    [Route("{userId}")]
    public async Task<ActionResult<OrderedCollectionPage>> GetFollowersPage(Guid userId,
        [FromQuery] int? page = null)
    {
        _logger.LogTrace($"Entered {nameof(GetFollowersPage)} in {nameof(FollowersController)}");

        var fullUserId = $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/actor/{userId}";

        if (page.IsNull()) return Ok(await GetFollowers(userId));

        var builder = Builders<Activity>.Sort;
        var sort = builder.Descending(i => i.Published);

        var filterBuilder = new FilterDefinitionBuilder<Activity>();
        var filter = filterBuilder.Where(i => i.Object!.StringLinks!.First() == fullUserId);

        var follows = (await _repository.GetSpecificPaged(DatabaseLocations.InboxFollow.Database,
            DatabaseLocations.InboxFollow.Collection, (int)page, 20, sort, filter)).ToList();
        
        var orderedCollection = new OrderedCollectionPage
        {
            Items = new TripleSet<Object>
            {
                Objects = follows
            },
            Id = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/followers/{userId}/?page={page}"),
            Next = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/followers/{userId}/?page={page + 1}" // TODO
                }
            },
            Prev = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/followers/{userId}/?page={page - 1}" // TODO
                }
            },
            PartOf = new TripleSet<OrderedCollection>
            {
                StringLinks = new[]
                {
                    $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/followers/{userId}" // TODO
                }
            },
            TotalItems = follows.Count
        };

        return Ok(orderedCollection);
    }
}