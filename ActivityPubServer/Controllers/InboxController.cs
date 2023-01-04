using System.Text;
using ActivityPubServer.Interfaces;
using ActivityPubServer.Model.ActivityPub;
using ActivityPubServer.Model.Helpers;
using CommonExtensions;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace ActivityPubServer.Controllers;

[Route("Inbox")]
public class InboxController : ControllerBase
{
    private readonly IHttpSignatureHandler _httpSignatureHandler;
    private readonly ILogger<InboxController> _logger;
    private readonly IMongoDbRepository _repository;

    public InboxController(ILogger<InboxController> logger, IHttpSignatureHandler httpSignatureHandler,
        IMongoDbRepository repository)
    {
        _logger = logger;
        _httpSignatureHandler = httpSignatureHandler;
        _repository = repository;
    }

    [HttpPost]
    public async Task<ActionResult> GeneralInbox([FromBody] Activity activity)
    {
        _logger.LogTrace($"Entered {nameof(GeneralInbox)} in {nameof(InboxController)}");

        if (!await _httpSignatureHandler.VerifySignature(HttpContext.Request.Headers, "/inbox"))
            return BadRequest("Invalid Signature");

        return Ok();
    }

    [HttpPost("{userId:guid}")]
    public async Task<ActionResult> Log(Guid userId, [FromBody] Activity activity)
    {
        _logger.LogTrace($"Entered {nameof(Log)} in {nameof(InboxController)}");
        
        if (!await _httpSignatureHandler.VerifySignature(HttpContext.Request.Headers, $"/inbox/{userId}"))
            return BadRequest("Invalid Signature");

        if (activity.IsNull())
        {
            return BadRequest("Activity can not be null!");
        }

        switch (activity.Type)
        {
            case "Create":
            {
                var post = activity.ExtractItemFromObject<Post>();
                
                _logger.LogDebug("Successfully extracted post from Object");
                
                break;
            }
            case "Follow":
            {
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

                    var followObject = new FollowingHelper()
                    {
                        Id = Guid.NewGuid(),
                        Following = new Uri((string)sendActivity.Object)
                    };

                    var followingDefinitionBuilder = Builders<FollowingHelper>.Filter;
                    var followingHelperFilter = followingDefinitionBuilder.Eq(i => i.Following, followObject.Following);
                    var fItem = await _repository.GetSpecificItems(followingHelperFilter, "Following", userId.ToString().ToLower());
                    
                    if (fItem.IsNullOrEmpty())
                    {
                        await _repository.Create(followObject, "Following", userId.ToString().ToLower());
                    }
                }
                else
                    _logger.LogWarning("Not found activity which was accepted");

                break;
            }
        }

        return Ok();
    }
}