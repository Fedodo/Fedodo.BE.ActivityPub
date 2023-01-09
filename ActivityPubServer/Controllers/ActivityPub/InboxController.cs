using ActivityPubServer.Interfaces;
using ActivityPubServer.Model.ActivityPub;
using ActivityPubServer.Model.Helpers;
using CommonExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using OpenIddict.Validation.AspNetCore;

namespace ActivityPubServer.Controllers.ActivityPub;

[Route("Inbox")]
public class InboxController : ControllerBase
{
    private readonly IActivityHandler _activityHandler;
    private readonly IHttpSignatureHandler _httpSignatureHandler;
    private readonly ILogger<InboxController> _logger;
    private readonly IMongoDbRepository _repository;
    private readonly IUserHandler _userHandler;

    public InboxController(ILogger<InboxController> logger, IHttpSignatureHandler httpSignatureHandler,
        IMongoDbRepository repository, IActivityHandler activityHandler, IUserHandler userHandler)
    {
        _logger = logger;
        _httpSignatureHandler = httpSignatureHandler;
        _repository = repository;
        _activityHandler = activityHandler;
        _userHandler = userHandler;
    }

    [HttpGet("{userId:guid}")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<ActionResult<OrderedCollection<Post>>> GetAllPostsInInbox(Guid userId)
    {
        if (!_userHandler.VerifyUser(userId, HttpContext)) return Forbid();

        var posts = await _repository.GetAll<Post>("Inbox", userId.ToString().ToLower());

        var orderedCollection = new OrderedCollection<Post>
        {
            Summary = $"Inbox of {userId}",
            OrderedItems = posts
        };

        return Ok(orderedCollection);
    }

    [HttpPost]
    public async Task<ActionResult> SharedInbox([FromBody] Activity activity)
    {
        _logger.LogTrace($"Entered {nameof(SharedInbox)} in {nameof(InboxController)}");

        if (!await _httpSignatureHandler.VerifySignature(HttpContext.Request.Headers, "/inbox"))
            return BadRequest("Invalid Signature");

        if (activity.IsNull())
        {
            _logger.LogWarning($"Activity is NULL in {nameof(SharedInbox)}");

            return BadRequest("Activity can not be null!");
        }

        return Ok();
    }

    [HttpPost("{userId:guid}")]
    public async Task<ActionResult> Inbox(Guid userId, [FromBody] Activity activity)
    {
        _logger.LogTrace($"Entered {nameof(Inbox)} in {nameof(InboxController)}");

        if (!await _httpSignatureHandler.VerifySignature(HttpContext.Request.Headers, $"/inbox/{userId}"))
            return BadRequest("Invalid Signature");

        if (activity.IsNull())
        {
            _logger.LogWarning($"Activity is NULL in {nameof(Inbox)}");

            return BadRequest("Activity can not be null!");
        }

        switch (activity.Type)
        {
            case "Create":
            {
                var post = activity.ExtractItemFromObject<Post>();

                _logger.LogDebug("Successfully extracted post from Object");

                var postDefinitionBuilder = Builders<Post>.Filter;
                var postFilter = postDefinitionBuilder.Eq(i => i.Id, post.Id);
                var fItem = await _repository.GetSpecificItems(postFilter, "Inbox", userId.ToString().ToLower());

                if (fItem.IsNotNullOrEmpty())
                    return BadRequest("Post already exists");

                await _repository.Create(post, "Inbox", userId.ToString().ToLower());

                break;
            }
            case "Follow":
            {
                _logger.LogDebug($"Got follow for \"{activity.ExtractStringFromObject()}\" from \"{activity.Actor}\"");

                var followObject = new FollowingHelper
                {
                    Id = Guid.NewGuid(),
                    Following = activity.Actor
                };

                var definitionBuilder = Builders<FollowingHelper>.Filter;
                var helperFilter = definitionBuilder.Eq(i => i.Following, followObject.Following);
                var fItem = await _repository.GetSpecificItems(helperFilter, "Followers", userId.ToString());

                if (fItem.IsNullOrEmpty())
                    await _repository.Create(followObject, "Followers", userId.ToString());

                var domainName = Environment.GetEnvironmentVariable("DOMAINNAME");
                var user = await _userHandler.GetUser(userId);
                var actor = await _activityHandler.GetActor(userId);

                var acceptActivity = new Activity
                {
                    Id = new Uri($"https://{domainName}/accepts/{Guid.NewGuid()}"),
                    Type = "Accept",
                    Actor = actor.Id,
                    Object = activity.Id,
                    To = new List<string>
                    {
                        //activity.Actor.ToString()
                        "as:Public"
                    }
                };

                await _activityHandler.SendActivities(acceptActivity, user, actor);

                break;
            }
            case "Accept":
            {
                _logger.LogTrace("Got an Accept activity");

                var acceptedActivity = activity.ExtractItemFromObject<Activity>();

                var actorDefinitionBuilder = Builders<Activity>.Filter;
                var filter = actorDefinitionBuilder.Eq(i => i.Id, acceptedActivity.Id);
                var sendActivity = await _repository.GetSpecificItem(filter, "Activities", userId.ToString().ToLower());

                if (sendActivity.IsNotNull())
                {
                    _logger.LogDebug("Found activity which was accepted");

                    var followObject = new FollowingHelper
                    {
                        Id = Guid.NewGuid(),
                        Following = new Uri((string)sendActivity.Object)
                    };

                    var followingDefinitionBuilder = Builders<FollowingHelper>.Filter;
                    var followingHelperFilter = followingDefinitionBuilder.Eq(i => i.Following, followObject.Following);
                    var fItem = await _repository.GetSpecificItems(followingHelperFilter, "Following",
                        userId.ToString().ToLower());

                    if (fItem.IsNullOrEmpty())
                        await _repository.Create(followObject, "Following", userId.ToString().ToLower());
                }
                else
                {
                    _logger.LogWarning("Not found activity which was accepted");
                }

                break;
            }
        }

        return Ok();
    }
}