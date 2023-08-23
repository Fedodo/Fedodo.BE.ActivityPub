using CommonExtensions;
using Fedodo.BE.ActivityPub.Constants;
using Fedodo.BE.ActivityPub.Extensions;
using Fedodo.BE.ActivityPub.Interfaces.Repositories;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.ActivityPub.Model.JsonConverters.Model;
using Microsoft.AspNetCore.Mvc;
using Object = Fedodo.NuGet.ActivityPub.Model.CoreTypes.Object;

namespace Fedodo.BE.ActivityPub.Controllers.ActivityPub;

[Route("Following")]
[Produces("application/json")]
public class FollowingController : ControllerBase
{
    private readonly IFollowingRepository _followingRepository;
    private readonly ILogger<FollowingController> _logger;

    public FollowingController(ILogger<FollowingController> logger, IFollowingRepository followingRepository)
    {
        _logger = logger;
        _followingRepository = followingRepository;
    }

    [HttpGet]
    [Route("{actorGuid}")]
    public async Task<ActionResult<OrderedCollectionPage>> GetFollowingsPage(Guid actorGuid,
        [FromQuery] int? page = null)
    {
        _logger.LogTrace($"Entered {nameof(GetFollowingsPage)} in {nameof(FollowingController)}");

        if (page.IsNull()) return Ok(await GetFollowings(actorGuid));

        var followings = await _followingRepository.GetFollowingsPageAsync(actorGuid.ToFullActorId(), (int)page);
        var activities = followings.ToList();

        var orderedCollection = new OrderedCollectionPage
        {
            Items = new TripleSet<Object>
            {
                Objects = activities
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
            TotalItems = activities.Count
        };

        return Ok(orderedCollection);
    }

    private async Task<OrderedCollection> GetFollowings(Guid actorGuid)
    {
        _logger.LogTrace($"Entered {nameof(GetFollowings)} in {nameof(FollowingController)}");

        var postCount = await _followingRepository.CountFollowingsAsync(actorGuid.ToFullActorId());

        var orderedCollection = new OrderedCollection
        {
            Id = new Uri($"https://{GeneralConstants.DomainName}/following/{actorGuid}"),
            First = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/following/{actorGuid}?page=0"
                }
            },
            Last = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/following/{actorGuid}?page={postCount / 20}"
                }
            },
            TotalItems = postCount
        };

        return orderedCollection;
    }
}