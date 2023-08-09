using CommonExtensions;
using Fedodo.BE.ActivityPub.Constants;
using Fedodo.BE.ActivityPub.Extensions;
using Fedodo.BE.ActivityPub.Interfaces.Repositories;
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
    private readonly IFollowerRepository _followerRepository;

    public FollowersController(ILogger<FollowersController> logger, IFollowerRepository followerRepository)
    {
        _logger = logger;
        _followerRepository = followerRepository;
    }

    [HttpGet]
    [Route("{actorGuid}")]
    public async Task<ActionResult<OrderedCollectionPage>> GetFollowersPage(Guid actorGuid, [FromQuery] int? page = null)
    {
        _logger.LogTrace($"Entered {nameof(GetFollowersPage)} in {nameof(FollowersController)}");
        
        if (page.IsNull()) return Ok(await GetFollowers(actorGuid));

        var follows = (await _followerRepository.GetFollowersPagedAsync(actorGuid.ToFullActorId(), (int)page)).ToList();

        var orderedCollection = new OrderedCollectionPage
        {
            Items = new TripleSet<Object>
            {
                Objects = follows
            },
            Id = new Uri($"https://{GeneralConstants.DomainName}/followers/{actorGuid}/?page={page}"),
            Next = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/followers/{actorGuid}/?page={page + 1}" // TODO
                }
            },
            Prev = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/followers/{actorGuid}/?page={page - 1}" // TODO
                }
            },
            PartOf = new TripleSet<OrderedCollection>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/followers/{actorGuid}" // TODO
                }
            },
            TotalItems = follows.Count
        };

        return Ok(orderedCollection);
    }
    
    private async Task<OrderedCollection> GetFollowers(Guid actorGuid)
    {
        _logger.LogTrace($"Entered {nameof(GetFollowers)} in {nameof(FollowersController)}");

        var postCount = await _followerRepository.CountFollowersAsync(actorGuid.ToFullActorId());

        var orderedCollection = new OrderedCollection
        {
            Id = new Uri($"https://{GeneralConstants.DomainName}/followers/{actorGuid}"),
            First = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/followers/{actorGuid}?page=0"
                }
            },
            Last = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/followers/{actorGuid}?page={postCount / 20}"
                }
            },
            TotalItems = postCount
        };

        return orderedCollection;
    }
}