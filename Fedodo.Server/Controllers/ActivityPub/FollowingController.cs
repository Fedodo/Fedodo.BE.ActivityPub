using System.Web;
using CommonExtensions;
using Fedodo.Server.Interfaces;
using Fedodo.Server.Model.ActivityPub;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Fedodo.Server.Controllers.ActivityPub;

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

    private async Task<ActionResult<OrderedPagedCollection>> GetFollowings(Guid userId)
    {
        _logger.LogTrace($"Entered {nameof(GetFollowings)} in {nameof(FollowingController)}");
        
        var filterBuilder = new FilterDefinitionBuilder<Activity>();
        var filter = filterBuilder.Where(i => (string)i.Object == userId.ToString());

        var postCount = await _repository.CountSpecific(DatabaseLocations.OutboxFollow.Database,
            DatabaseLocations.OutboxFollow.Collection, filter);
        
        var orderedCollection = new OrderedPagedCollection
        {
            Id = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/followings/{userId}"),
            TotalItems = postCount,
            First = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/followings/{userId}?page=0"),
            Last = new Uri(
                $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/followings/{userId}?page={postCount / 20}")
        };

        return orderedCollection;
    }

    [HttpGet]
    [Route($"{{userId}}")]
    public async Task<ActionResult<OrderedCollectionPage<Activity>>> GetFollowingsPage(Guid userId, [FromQuery]int? page = null)
    {
        _logger.LogTrace($"Entered {nameof(GetFollowingsPage)} in {nameof(FollowingController)}");

        if (page.IsNull())
        {
            return Ok(await GetFollowings(userId: userId));
        }
        
        var builder = Builders<Activity>.Sort;
        var sort = builder.Descending(i => i.Published);

        var filterBuilder = new FilterDefinitionBuilder<Activity>();
        var filter = filterBuilder.Where(i => (string)i.Object == userId.ToString());
        
        var likes = (await _repository.GetSpecificPaged(DatabaseLocations.OutboxFollow.Database,
            DatabaseLocations.OutboxFollow.Collection, (int)page, 20, sort, filter)).ToList();

        var orderedCollection = new OrderedCollectionPage<Activity>
        {
            OrderedItems = likes,
            Id = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/followings/{userId}/?page={page}"),
            Next = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/followings/{userId}/?page={page + 1}"), // TODO
            Prev = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/followings/{userId}/?page={page - 1}"), // TODO
            PartOf = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/followings/{userId}")
        };

        return Ok(orderedCollection);
    }
}