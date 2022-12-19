using System.Security.Claims;
using CommonExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ActivityPubServer.Controllers;

[Route("outbox")]
public class OutboxController : ControllerBase
{
    private readonly ILogger<OutboxController> _logger;

    public OutboxController(ILogger<OutboxController> logger)
    {
        _logger = logger;
    }
    
    [HttpPost("{userId}")]
    [Authorize(Roles = "User")]
    public ActionResult CreatePost(Guid userId)
    {
        var activeUserClaims = HttpContext.User.Claims.ToList();
        var tokenUserId = activeUserClaims.Where(i => i.ValueType.IsNotNull() && i.Type == ClaimTypes.Sid)?.First().Value;
        
        if (tokenUserId != userId.ToString())
        {
            _logger.LogWarning($"Someone tried to post as {userId} but was authorized as {tokenUserId}");
            return Forbid($"You are not {userId}");
        }
        
        // TODO
        
        return Ok();
    }
}