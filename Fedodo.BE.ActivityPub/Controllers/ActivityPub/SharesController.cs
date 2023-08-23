using System.Web;
using CommonExtensions;
using Fedodo.BE.ActivityPub.Constants;
using Fedodo.BE.ActivityPub.Interfaces.Repositories;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.ActivityPub.Model.JsonConverters.Model;
using Microsoft.AspNetCore.Mvc;
using Object = Fedodo.NuGet.ActivityPub.Model.CoreTypes.Object;

namespace Fedodo.BE.ActivityPub.Controllers.ActivityPub;

[Route("Shares")]
[Produces("application/json")]
public class SharesController : ControllerBase
{
    private readonly ILogger<SharesController> _logger;
    private readonly ISharesRepository _sharesRepository;

    public SharesController(ILogger<SharesController> logger, ISharesRepository sharesRepository)
    {
        _logger = logger;
        _sharesRepository = sharesRepository;
    }

    [HttpGet]
    [Route("{postIdUrlEncoded}")]
    public async Task<ActionResult<OrderedCollectionPage>> GetSharesPage(string postIdUrlEncoded,
        [FromQuery] int? page = null)
    {
        _logger.LogTrace($"Entered {nameof(GetSharesPage)} in {nameof(SharesController)}");

        if (page.IsNull()) return Ok(await GetSharesSummary(postIdUrlEncoded));

        var postId = HttpUtility.UrlDecode(postIdUrlEncoded);

        var shares = await _sharesRepository.GetSharesAsync(postId, (int)page);

        var orderedCollection = new OrderedCollectionPage
        {
            Items = new TripleSet<Object>
            {
                Objects = shares
            },
            Id = new Uri(
                $"https://{GeneralConstants.DomainName}/shares/{HttpUtility.UrlEncode(postId)}/?page={page}"),
            Next = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/shares/{HttpUtility.UrlEncode(postId)}/?page={page + 1}" // TODO
                }
            },
            Prev = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/shares/{HttpUtility.UrlEncode(postId)}/?page={page - 1}" // TODO
                }
            },
            PartOf = new TripleSet<OrderedCollection>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/shares/{HttpUtility.UrlEncode(postId)}" // TODO
                }
            },
            TotalItems = shares.Count
        };

        return Ok(orderedCollection);
    }

    private async Task<OrderedCollection> GetSharesSummary(string postIdUrlEncoded)
    {
        _logger.LogTrace($"Entered {nameof(GetSharesSummary)} in {nameof(SharesController)}");

        var postId = HttpUtility.UrlDecode(postIdUrlEncoded);

        var count = await _sharesRepository.CountAsync(postId);

        var orderedCollection = new OrderedCollection
        {
            Id = new Uri(
                $"https://{GeneralConstants.DomainName}/shares/{HttpUtility.UrlEncode(postId)}"),
            First = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/shares/{HttpUtility.UrlEncode(postId)}?page=0" // TODO
                }
            },
            Last = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/shares/{HttpUtility.UrlEncode(postId)}?page={count / 20}" // TODO
                }
            },
            TotalItems = count
        };

        return orderedCollection;
    }
}