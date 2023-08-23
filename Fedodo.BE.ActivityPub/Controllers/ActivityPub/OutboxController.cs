using CommonExtensions;
using Fedodo.BE.ActivityPub.Constants;
using Fedodo.BE.ActivityPub.Extensions;
using Fedodo.BE.ActivityPub.Interfaces.Repositories;
using Fedodo.BE.ActivityPub.Interfaces.Services;
using Fedodo.BE.ActivityPub.Model.DTOs;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.ActivityPub.Model.JsonConverters.Model;
using Fedodo.NuGet.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Object = Fedodo.NuGet.ActivityPub.Model.CoreTypes.Object;

namespace Fedodo.BE.ActivityPub.Controllers.ActivityPub;

[Route("Outbox")]
[Produces("application/json")]
public class OutboxController : ControllerBase
{
    private readonly ICreateActivityService _createActivityService;
    private readonly ILogger<OutboxController> _logger;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IUserHandler _userHandler;
    private readonly IUserRepository _userRepository;

    public OutboxController(ILogger<OutboxController> logger, ICreateActivityService createActivityService,
        IUserHandler userHandler, IOutboxRepository outboxRepository, IUserRepository userRepository)
    {
        _logger = logger;
        _createActivityService = createActivityService;
        _userHandler = userHandler;
        _outboxRepository = outboxRepository;
        _userRepository = userRepository;
    }

    [HttpGet("{actorId:guid}")]
    public async Task<ActionResult<OrderedCollection>> GetPublicPostsPageInformation(Guid actorId)
    {
        var postCount = await _outboxRepository.CountOutboxActivitiesAsync(actorId.ToFullActorId());

        var orderedCollection = new OrderedCollection
        {
            Id = new Uri($"https://{GeneralConstants.DomainName}/outbox/{actorId}"),
            First = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/outbox/{actorId}/page/0"
                }
            },
            Last = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/outbox/{actorId}/page/{postCount / 20}"
                }
            },
            TotalItems = postCount
        };

        return Ok(orderedCollection);
    }

    [HttpGet("{actorId:guid}/page/{pageId:int}")]
    public async Task<ActionResult<OrderedCollectionPage>> GetPublicPage(Guid actorId, int pageId)
    {
        var page = (await _outboxRepository.GetPagedAsync(actorId.ToFullActorId(), pageId))
            .ToList(); // Enumerate once to prevent multiple enumerations

        var previousPageId = pageId - 1;
        if (previousPageId < 0) previousPageId = 0;
        var nextPageId = pageId + 1;
        // TODO if (nextPageId > ) nextPageId = 

        var orderedCollectionPage = new OrderedCollectionPage
        {
            Id = new Uri($"https://{GeneralConstants.DomainName}/outbox/{actorId}/page/{pageId}"),
            PartOf = new TripleSet<OrderedCollection>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/outbox/{actorId}"
                }
            },
            Prev = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/outbox/{actorId}/page/{previousPageId}"
                }
            },
            Next = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/outbox/{actorId}/page/{nextPageId}"
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

    [HttpPost("{actorId:guid}")]
    [Authorize]
    public async Task<ActionResult<Activity>> CreatePost(Guid actorId, [FromBody] CreateActivityDto activityDto)
    {
        _logger.LogTrace($"Entered {nameof(CreatePost)} in {nameof(OutboxController)}");
        if (!_userHandler.VerifyActorId(actorId, HttpContext)) return Forbid();

        if (activityDto.IsNull()) return BadRequest("Activity can not be null");

        var domainName = GeneralConstants.DomainName!;

        var actorSecrets = await _userRepository.GetActorSecretsAsync(actorId.ToFullActorId());

        if (actorSecrets.IsNull())
        {
            _logger.LogCritical($"{nameof(actorSecrets)} is null for {nameof(actorId)}: \"{actorId}\"");
            return BadRequest("ActorId is not correct");
        }

        var actor = await _userRepository.GetActorByIdAsync(actorId.ToFullActorId());
        var activity = await _createActivityService.CreateActivity(actorId.ToFullActorId(), activityDto);

        if (activity.IsNull()) return BadRequest("Activity could not be created. Check if Activity Type is supported.");

        await _createActivityService.SendActivitiesAsync(activity, actorSecrets, actor);

        return Created(activity.Id, activity);
    }
}