using System.Web;
using CommonExtensions;
using Fedodo.BE.ActivityPub.Constants;
using Fedodo.BE.ActivityPub.Interfaces.Repositories;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.ActivityPub.Model.JsonConverters.Model;
using Microsoft.AspNetCore.Mvc;
using Object = Fedodo.NuGet.ActivityPub.Model.CoreTypes.Object;

namespace Fedodo.BE.ActivityPub.Controllers.ActivityPub;

[Route("Likes")]
[Produces("application/json")]
public class LikesController : ControllerBase
{
    private readonly ILikesRepository _likesRepository;
    private readonly ILogger<LikesController> _logger;

    public LikesController(ILogger<LikesController> logger, ILikesRepository likesRepository)
    {
        _logger = logger;
        _likesRepository = likesRepository;
    }

    private async Task<OrderedCollection> GetLikes(string postIdUrlEncoded)
    {
        _logger.LogTrace($"Entered {nameof(GetLikes)} in {nameof(LikesController)}");

        var postId = HttpUtility.UrlDecode(postIdUrlEncoded);

        var count = await _likesRepository.CountLikesAsync(postId);

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
                    $"https://{GeneralConstants.DomainName}/likes/{HttpUtility.UrlEncode(postId)}?page={count / 20}"
                }
            },
            TotalItems = count
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

        var likes = (await _likesRepository.GetLikesPagedAsync(postId, (int)page)).ToList();

        var orderedCollection = new OrderedCollectionPage
        {
            Items = new TripleSet<Object>
            {
                Objects = likes
            },
            Id = new Uri(
                $"https://{GeneralConstants.DomainName}/likes/{postIdUrlEncoded}/?page={page}"),
            Next = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/likes/{postIdUrlEncoded}/?page={page + 1}" // TODO
                }
            },
            Prev = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/likes/{postIdUrlEncoded}/?page={page - 1}" // TODO
                }
            },
            PartOf = new TripleSet<OrderedCollection>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/likes/{postIdUrlEncoded}"
                }
            },
            TotalItems = likes.Count
        };

        return Ok(orderedCollection);
    }
}