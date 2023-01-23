using CommonExtensions;
using Fedido.Server.Interfaces;
using Fedido.Server.Model.ActivityPub;
using Fedido.Server.Model.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using OpenIddict.Validation.AspNetCore;

namespace Fedido.Server.Controllers.ActivityPub;

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

        var posts = await _repository.GetAll<Post>(DatabaseLocations.InboxNotes.Database,
            DatabaseLocations.InboxNotes.Collection);

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
                Post? post = null;

                try
                {
                    _logger.LogDebug($"Create object type is {activity.Object.GetType()}");
                    post = activity.Object.TrySystemJsonDeserialization<Post>();
                }
                catch (ArgumentException ex)
                {
                    _logger.LogError(ex, "Could not extract Post from Object.");

                    return BadRequest("Could not extract Post from Object.");
                }

                _logger.LogDebug("Successfully extracted post from Object");

                var postDefinitionBuilder = Builders<Post>.Filter;
                var postFilter = postDefinitionBuilder.Eq(i => i.Id, post.Id);
                var fItem = await _repository.GetSpecificItems(postFilter, DatabaseLocations.InboxNotes.Database,
                    DatabaseLocations.InboxNotes.Collection);

                if (fItem.IsNotNullOrEmpty())
                    return BadRequest("Post already exists");

                await _repository.Create(post, DatabaseLocations.InboxNotes.Database,
                    DatabaseLocations.InboxNotes.Collection);

                break;
            }
            case "Follow":
            {
                _logger.LogDebug(
                    $"Got follow for \"{activity.Object.TrySystemJsonDeserialization<string>()}\" from \"{activity.Actor}\"");

                var followObject = new FollowingHelper
                {
                    Id = Guid.NewGuid(),
                    Following = activity.Actor
                };

                var definitionBuilder = Builders<FollowingHelper>.Filter;
                var helperFilter = definitionBuilder.Eq(i => i.Following, followObject.Following);
                var fItem = await _repository.GetSpecificItems(helperFilter, DatabaseLocations.Followers.Database,
                    userId.ToString());

                if (fItem.IsNullOrEmpty())
                    await _repository.Create(followObject, DatabaseLocations.Followers.Database, userId.ToString());

                var domainName = Environment.GetEnvironmentVariable("DOMAINNAME");
                var user = await _userHandler.GetUserByIdAsync(userId);
                var actor = await _activityHandler.GetActorAsync(userId, domainName);

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

                var acceptedActivity = activity.Object.TrySystemJsonDeserialization<Activity>();

                var actorDefinitionBuilder = Builders<Activity>.Filter;
                var filter = actorDefinitionBuilder.Eq(i => i.Id, acceptedActivity.Id);
                var sendActivity = await _repository.GetSpecificItem(filter, DatabaseLocations.Activities.Database,
                    userId.ToString().ToLower());

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
                    var fItem = await _repository.GetSpecificItems(followingHelperFilter,
                        DatabaseLocations.Followings.Database,
                        userId.ToString().ToLower());

                    if (fItem.IsNullOrEmpty())
                        await _repository.Create(followObject, DatabaseLocations.Followings.Database,
                            userId.ToString().ToLower());
                }
                else
                {
                    _logger.LogWarning("Not found activity which was accepted");
                }

                break;
            }
            case "Announce":
            {
                _logger.LogTrace("Got an Announce Activity");

                var share = new ShareHelper
                {
                    Share = activity.Actor
                };

                var postId = new Uri(activity.Object.TrySystemJsonDeserialization<string>()).AbsolutePath
                    .Replace("posts", "").Replace("/", "");

                var definitionBuilder = Builders<ShareHelper>.Filter;
                var filter = definitionBuilder.Eq(i => i.Share, activity.Actor);
                var fItem = await _repository.GetSpecificItems(filter, DatabaseLocations.Shares.Database, postId);

                if (fItem.IsNullOrEmpty())
                    await _repository.Create(share, DatabaseLocations.Shares.Database, postId);
                else
                    _logger.LogWarning("Got another share of the same actor.");

                break;
            }
            case "Like":
            {
                _logger.LogTrace("Got an Like Activity");

                var like = new LikeHelper
                {
                    Like = activity.Actor
                };

                var postId = new Uri(activity.Object.TrySystemJsonDeserialization<string>()).AbsolutePath
                    .Replace("posts", "").Replace("/", "");

                var definitionBuilder = Builders<LikeHelper>.Filter;
                var filter = definitionBuilder.Eq(i => i.Like, activity.Actor);
                var fItem = await _repository.GetSpecificItems(filter, DatabaseLocations.Likes.Database, postId);

                if (fItem.IsNullOrEmpty())
                    await _repository.Create(like, DatabaseLocations.Likes.Database, postId);
                else
                    _logger.LogWarning("Got another like of the same actor.");

                break;
            }
            case "Update":
            {
                var post = activity.Object.TrySystemJsonDeserialization<Post>();

                _logger.LogDebug("Successfully extracted post from Object");

                var postDefinitionBuilder = Builders<Post>.Filter;
                var postFilter = postDefinitionBuilder.Eq(i => i.Id, post.Id);

                await _repository.Update(post, postFilter, DatabaseLocations.InboxNotes.Database,
                    DatabaseLocations.InboxNotes.Collection);

                break;
            }
            case "Undo":
            {
                var undoActivity = activity.Object.TrySystemJsonDeserialization<Activity>();
                var undoActivityObject = new Uri(undoActivity.Object.TrySystemJsonDeserialization<string>());

                switch (undoActivity.Type)
                {
                    case "Like":
                    {
                        _logger.LogTrace("Got undoActivity of type Like");

                        var postId = undoActivityObject.AbsolutePath
                            .Replace("posts", "").Replace("/", "");

                        var definitionBuilder = Builders<LikeHelper>.Filter;
                        var filter = definitionBuilder.Eq(i => i.Like, activity.Actor);
                        var fItem = await _repository.GetSpecificItems(filter, DatabaseLocations.Likes.Database,
                            postId);

                        if (fItem.IsNotNullOrEmpty())
                            await _repository.Delete(filter, DatabaseLocations.Likes.Database, postId);
                        else
                            _logger.LogWarning("Got no like of the same actor.");

                        break;
                    }
                    case "Announce":
                    {
                        _logger.LogTrace("Got an Undo Announce Activity");

                        var postId = undoActivityObject.AbsolutePath
                            .Replace("posts", "").Replace("/", "");

                        var definitionBuilder = Builders<ShareHelper>.Filter;
                        var filter = definitionBuilder.Eq(i => i.Share, activity.Actor);
                        var fItem = await _repository.GetSpecificItems(filter, DatabaseLocations.Shares.Database,
                            postId);

                        if (fItem.IsNotNullOrEmpty())
                            await _repository.Delete(filter, DatabaseLocations.Shares.Database, postId);
                        else
                            _logger.LogWarning("Found no share of this actor to undo.");

                        break;
                    }
                }

                break;
            }
            case "Delete":
            {
                var postId = new Uri(activity.Object.TrySystemJsonDeserialization<string>());

                var definitionBuilder = Builders<Post>.Filter;
                var filter = definitionBuilder.Eq(i => i.Id, postId);

                var post = await _repository.GetSpecificItem(filter, DatabaseLocations.InboxNotes.Database,
                    DatabaseLocations.InboxNotes.Collection);

                if (post.AttributedTo == activity.Actor)
                    await _repository.Delete(filter, DatabaseLocations.InboxNotes.Database,
                        DatabaseLocations.InboxNotes.Collection);

                break;
            }
        }

        return Ok();
    }
}