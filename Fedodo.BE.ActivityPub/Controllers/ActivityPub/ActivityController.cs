using Fedodo.BE.ActivityPub.Extensions;
using Fedodo.BE.ActivityPub.Interfaces.Repositories;
using Fedodo.NuGet.ActivityPub.Model.ObjectTypes;
using Microsoft.AspNetCore.Mvc;

namespace Fedodo.BE.ActivityPub.Controllers.ActivityPub;

[Route("Activities")]
[Produces("application/json")]
public class ActivityController : ControllerBase
{
    private readonly IActivityRepository _activityRepository;
    private readonly ILogger<ActivityController> _logger;

    public ActivityController(ILogger<ActivityController> logger, IActivityRepository activityRepository)
    {
        _logger = logger;
        _activityRepository = activityRepository;
    }

    [HttpGet("{activityId:guid}")]
    public async Task<ActionResult<Note>> GetPost(Guid activityId)
    {
        var post = await _activityRepository.GetActivityByIdAsync(activityId.ToFullActorId());

        if (post.IsActivityPublic())
            return Ok(post);

        return Forbid("Not a public post");
    }
}