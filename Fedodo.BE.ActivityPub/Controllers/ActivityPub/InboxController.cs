using CommonExtensions;
using Fedodo.BE.ActivityPub.Constants;
using Fedodo.BE.ActivityPub.Extensions;
using Fedodo.BE.ActivityPub.Interfaces;
using Fedodo.BE.ActivityPub.Interfaces.Repositories;
using Fedodo.BE.ActivityPub.Interfaces.Services;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.ActivityPub.Model.JsonConverters.Model;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Object = Fedodo.NuGet.ActivityPub.Model.CoreTypes.Object;

namespace Fedodo.BE.ActivityPub.Controllers.ActivityPub;

[Route("Inbox")]
[Produces("application/json")]
public class InboxController : ControllerBase
{
    private readonly IHttpSignatureService _httpSignatureService;
    private readonly ILogger<InboxController> _logger;
    private readonly IUserHandler _userHandler;
    private readonly IInboxRepository _inboxRepository;
    private readonly IInboxService _inboxService;

    public InboxController(ILogger<InboxController> logger, IHttpSignatureService httpSignatureService,
        IUserHandler userHandler, IInboxRepository inboxRepository, IInboxService inboxService)
    {
        _logger = logger;
        _httpSignatureService = httpSignatureService;
        _userHandler = userHandler;
        _inboxRepository = inboxRepository;
        _inboxService = inboxService;
    }

    [HttpGet("{actorGuid:guid}")]
    [Authorize]
    public async Task<ActionResult<OrderedCollection>> GetInboxPageInformation(Guid actorGuid)
    {
        if (!_userHandler.VerifyActorId(actorGuid, HttpContext)) return Forbid();

        var postCount = await _inboxRepository.CountInboxItemsAsync(actorGuid.ToFullActorId());

        var orderedCollection = new OrderedCollection
        {
            Id = new Uri($"https://{GeneralConstants.DomainName}/inbox/{actorGuid}"),
            First = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/inbox/{actorGuid}/page/0"
                }
            },
            Last = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/inbox/{actorGuid}/page/{postCount / 20}"
                }
            },
            TotalItems = postCount
        };

        return Ok(orderedCollection);
    }

    [HttpGet("{actorGuid:guid}/page/{pageId:int}")]
    [Authorize]
    public async Task<ActionResult<OrderedCollectionPage>> GetPageInInbox(Guid actorGuid, int pageId)
    {
        if (!_userHandler.VerifyActorId(actorGuid, HttpContext)) return Forbid();

        var page = await _inboxRepository.GetPagedAsync(actorGuid.ToFullActorId(), pageId);

        var previousPageId = pageId - 1;
        if (previousPageId < 0) previousPageId = 0;
        var nextPageId = pageId + 1;
        // TODO if (nextPageId > ) nextPageId = 

        var orderedCollectionPage = new OrderedCollectionPage
        {
            Id = new Uri($"https://{GeneralConstants.DomainName}/inbox/{actorGuid}/page/{pageId}"),
            PartOf = new TripleSet<OrderedCollection>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/inbox/{actorGuid}"
                }
            },
            Prev = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/inbox/{actorGuid}/page/{previousPageId}"
                }
            },
            Next = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/inbox/{actorGuid}/page/{nextPageId}"
                }
            },
            Items = new TripleSet<Object>
            {
                Objects = page
            },
            TotalItems = page.Count
        };

        return Ok(orderedCollectionPage);
    }

    [HttpPost("")]
    public async Task<ActionResult> SharedInbox([FromBody] Activity activity)
    {
        _logger.LogTrace($"Entered {nameof(SharedInbox)} in {nameof(InboxController)}");

        if (!await _httpSignatureService.VerifySignature(HttpContext.Request.Headers, "/inbox"))
            return BadRequest("Invalid Signature");

        if (!activity.IsNull()) return Ok();

        _logger.LogWarning($"Activity is NULL in {nameof(SharedInbox)}");
        return BadRequest("Activity can not be null!");
    }

    [HttpPost("{actorGuid:guid}")]
    public async Task<ActionResult> Inbox(Guid actorGuid, [FromBody] Activity activity)
    {
        _logger.LogTrace($"Entered {nameof(Inbox)} in {nameof(InboxController)}");

        if (!await _httpSignatureService.VerifySignature(HttpContext.Request.Headers, $"/inbox/{actorGuid}"))
            return BadRequest("Invalid Signature");

        try
        {
            await _inboxService.ActivityReceived(activity, actorGuid.ToFullActorId());
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, $"Error caught in {nameof(Inbox)}");
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, $"Error caught in {nameof(Inbox)}");
            return BadRequest(ex.Message);
        }

        return Ok();
    }
}