using CommonExtensions;
using Fedodo.BE.ActivityPub.Model.ActivityPub;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Fedodo.BE.ActivityPub.Controllers.ActivityPub;

[Route("Following")]
public class FollowingController : ControllerBase
{
    private readonly ILogger<FollowingController> _logger;
    private readonly IMongoDbRepository _repository;

    public FollowingController(ILogger<FollowingController> logger, IMongoDbRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    private async Task<OrderedPagedCollection> GetFollowings(Guid userId)
    {
        _logger.LogTrace($"Entered {nameof(GetFollowings)} in {nameof(FollowingController)}");

        var fullUserId = $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/actor/{userId}";

        var filterBuilder = new FilterDefinitionBuilder<Activity>();
        var filter = filterBuilder.Where(i => i.Actor.ToString() == fullUserId);

        var postCount = await _repository.CountSpecific(DatabaseLocations.OutboxFollow.Database,
            DatabaseLocations.OutboxFollow.Collection, filter);

        var orderedCollection = new OrderedPagedCollection
        {
            Id = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/following/{userId}"),
            TotalItems = postCount,
            First = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/following/{userId}?page=0"),
            Last = new Uri(
                $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/following/{userId}?page={postCount / 20}")
        };

        return orderedCollection;
    }

    [HttpGet]
    [Route("{userId}")]
    public async Task<ActionResult<OrderedCollectionPage<Activity>>> GetFollowingsPage(Guid userId,
        [FromQuery] int? page = null)
    {
        _logger.LogTrace($"Entered {nameof(GetFollowingsPage)} in {nameof(FollowingController)}");

        var fullUserId = $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/actor/{userId}";

        if (page.IsNull()) return Ok(await GetFollowings(userId));

        var builder = Builders<Activity>.Sort;
        var sort = builder.Descending(i => i.Published);

        var filterBuilder = new FilterDefinitionBuilder<Activity>();
        var filter = filterBuilder.Where(i => i.Actor.ToString() == fullUserId);

        var followings = (await _repository.GetSpecificPaged(DatabaseLocations.OutboxFollow.Database,
            DatabaseLocations.OutboxFollow.Collection, (int)page, 20, sort, filter)).ToList();

        var orderedCollection = new OrderedCollectionPage<Activity>
        {
            OrderedItems = followings,
            Id = new Uri(
                $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/following/{userId}/?page={page}"),
            Next = new Uri(
                $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/following/{userId}/?page={page + 1}"), // TODO
            Prev = new Uri(
                $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/following/{userId}/?page={page - 1}"), // TODO
            PartOf = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/following/{userId}")
        };

        return Ok(orderedCollection);
    }
}