using CommonExtensions;
using Fedodo.NuGet.ActivityPub.Model;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Fedodo.BE.ActivityPub.Controllers.ActivityPub;

[Route("Followers")]
public class FollowersController : ControllerBase
{
    private readonly ILogger<FollowersController> _logger;
    private readonly IMongoDbRepository _repository;

    public FollowersController(ILogger<FollowersController> logger, IMongoDbRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    private async Task<OrderedPagedCollection> GetFollowers(Guid userId)
    {
        _logger.LogTrace($"Entered {nameof(GetFollowers)} in {nameof(FollowersController)}");

        var fullUserId = $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/actor/{userId}";

        var filterBuilder = new FilterDefinitionBuilder<Activity>();
        var filter = filterBuilder.Where(i => (string)i.Object == fullUserId);

        var postCount = await _repository.CountSpecific(DatabaseLocations.InboxFollow.Database,
            DatabaseLocations.InboxFollow.Collection, filter);

        var orderedCollection = new OrderedPagedCollection
        {
            Id = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/followers/{userId}"),
            TotalItems = postCount,
            First = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/followers/{userId}?page=0"),
            Last = new Uri(
                $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/followers/{userId}?page={postCount / 20}")
        };

        return orderedCollection;
    }

    [HttpGet]
    [Route("{userId}")]
    public async Task<ActionResult<OrderedCollectionPage<Activity>>> GetFollowersPage(Guid userId,
        [FromQuery] int? page = null)
    {
        _logger.LogTrace($"Entered {nameof(GetFollowersPage)} in {nameof(FollowersController)}");

        var fullUserId = $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/actor/{userId}";

        if (page.IsNull()) return Ok(await GetFollowers(userId));

        var builder = Builders<Activity>.Sort;
        var sort = builder.Descending(i => i.Published);

        var filterBuilder = new FilterDefinitionBuilder<Activity>();
        var filter = filterBuilder.Where(i => (string)i.Object == fullUserId);

        var likes = (await _repository.GetSpecificPaged(DatabaseLocations.InboxFollow.Database,
            DatabaseLocations.InboxFollow.Collection, (int)page, 20, sort, filter)).ToList();

        var orderedCollection = new OrderedCollectionPage<Activity>
        {
            OrderedItems = likes,
            Id = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/followers/{userId}/?page={page}"),
            Next = new Uri(
                $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/followers/{userId}/?page={page + 1}"), // TODO
            Prev = new Uri(
                $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/followers/{userId}/?page={page - 1}"), // TODO
            PartOf = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/followers/{userId}")
        };

        return Ok(orderedCollection);
    }
}