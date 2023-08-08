using CommonExtensions;
using Fedodo.BE.ActivityPub.Constants;
using Fedodo.BE.ActivityPub.Interfaces.Repositories;
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
    private readonly IFollowingRepository _followingRepository;

    public FollowingController(ILogger<FollowingController> logger, IFollowingRepository followingRepository)
    {
        _logger = logger;
        _followingRepository = followingRepository;
    }

    private async Task<OrderedCollection> GetFollowings(Guid actorId)
    {
        _logger.LogTrace($"Entered {nameof(GetFollowings)} in {nameof(FollowingController)}");

        var fullUserId = $"https://{GeneralConstants.DomainName}/actor/{actorId}";

        var filterBuilder = new FilterDefinitionBuilder<Activity>();
        var filter = filterBuilder.Where(i =>
            i.Type == "Follow" && i.Actor != null && i.Actor.StringLinks != null &&
            i.Actor.StringLinks.ToList()[0].ToString() == fullUserId);

        var postCount = await _repository.CountSpecific(DatabaseLocations.Activity.Database, fullUserId, filter);

        var orderedCollection = new OrderedCollection
        {
            Id = new Uri($"https://{GeneralConstants.DomainName}/following/{actorId}"),
            First = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/following/{actorId}?page=0"
                }
            },
            Last = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/following/{actorId}?page={postCount / 20}"
                }
            },
            TotalItems = postCount
        };

        return orderedCollection;
    }

    [HttpGet]
    [Route("{actorGuid}")]
    public async Task<ActionResult<OrderedCollectionPage>> GetFollowingsPage(Guid actorGuid,
        [FromQuery] int? page = null)
    {
        _logger.LogTrace($"Entered {nameof(GetFollowingsPage)} in {nameof(FollowingController)}");

        var fullUserId = $"https://{GeneralConstants.DomainName}/actor/{actorGuid}";

        if (page.IsNull()) return Ok(await GetFollowings(actorGuid));

        var followings = await _followingRepository.GetFollowingsPage(fullUserId, (int)page);

        var orderedCollection = new OrderedCollectionPage
        {
            Items = new TripleSet<Object>
            {
                Objects = followings
            },
            Id = new Uri($"https://{GeneralConstants.DomainName}/following/{actorGuid}/?page={page}"),
            Next = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/following/{actorGuid}/?page={page + 1}" // TODO
                }
            },
            Prev = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/following/{actorGuid}/?page={page - 1}" // TODO
                }
            },
            PartOf = new TripleSet<OrderedCollection>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/following/{actorGuid}" // TODO
                }
            },
            TotalItems = followings.Count
        };

        return Ok(orderedCollection);
    }

    internal async Task<List<string>> GetAllFollowings(Guid actorGuid)
    {
        List<string> links = new();
        
        var followingsInfo = await GetFollowings(actorGuid);
        var totalItems = followingsInfo.TotalItems;

        var counter = 0;
        while (totalItems > links.Count)
        {
            var followingPage = (await GetFollowingsPage(actorGuid, counter)).Value;

            if (followingPage?.Items?.StringLinks.IsNullOrEmpty() ?? true)
            {
                continue;
            }
            
            links.AddRange(followingPage.Items.StringLinks);

            counter++;
        }

        return links;
    }
}