using System.Text;
using ActivityPubServer.Model.ActivityPub;
using Microsoft.AspNetCore.Mvc;

namespace ActivityPubServer.Controllers;

[Route("Inbox")]
public class InboxController : ControllerBase
{
    private readonly ILogger<InboxController> _logger;

    public InboxController(ILogger<InboxController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult> Log([FromBody]Activity activity)
    {
        if (!await VerifySignature(HttpContext.Request.Headers))
        {
            return BadRequest("Invalid Signature");
        }

        return Ok();
    }

    [HttpPost("{userId}")]
    public async Task<ActionResult> Log(Guid userId, [FromBody]Activity activity)
    {
        if (!await VerifySignature(HttpContext.Request.Headers))
        {
            return BadRequest("Invalid Signature");
        }


        return Ok();
    }

    private async Task<bool> VerifySignature(IHeaderDictionary requestHeaders)
    {
        _logger.LogTrace("Verifying Signature");
        
        
        // TODO Digit Header
        
        
        var signatureHeader = requestHeaders["Signature"].First().Split(",").ToList();

        var keyId = new Uri(signatureHeader.FirstOrDefault(i => i.StartsWith("keyId"))?.Replace("keyId=", "")
            .Replace("\"", "") ?? string.Empty);
        var headers = signatureHeader.FirstOrDefault(i => i.StartsWith("headers"))?.Replace("headers=", "")
            .Replace("\"", "");
        var signature = signatureHeader.FirstOrDefault(i => i.StartsWith("signature"))?.Replace("signature=", "")
            .Replace("\"", ""); // TODO Maybe converted to BASE 64

        var http = new HttpClient();
        var response = await http.GetAsync(keyId);
        var resultActor = await response.Content.ReadFromJsonAsync<Actor>();

        return false; // TODO
    }
}