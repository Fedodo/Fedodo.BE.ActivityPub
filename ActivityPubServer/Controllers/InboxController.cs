using ActivityPubServer.Interfaces;
using ActivityPubServer.Model.ActivityPub;
using Microsoft.AspNetCore.Mvc;

namespace ActivityPubServer.Controllers;

[Route("Inbox")]
public class InboxController : ControllerBase
{
    private readonly IHttpSignatureHandler _httpSignatureHandler;
    private readonly ILogger<InboxController> _logger;

    public InboxController(ILogger<InboxController> logger, IHttpSignatureHandler httpSignatureHandler)
    {
        _logger = logger;
        _httpSignatureHandler = httpSignatureHandler;
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

        switch (activity.Type)
        {
            case "Create":
            {
                break;
            }
            case "Follow":
            {
                break;
            }
            case "Accept":
            {
                var acceptedItemString = activity.ExtractStringFromObject();
                
                _logger.LogDebug($"acceptedItemString:{acceptedItemString}");
                
                break;
            }
        }

        return Ok();
    }
}