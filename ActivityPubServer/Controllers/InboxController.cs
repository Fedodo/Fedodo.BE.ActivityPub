using System.Security.Cryptography;
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
    public async Task<ActionResult> GeneralInbox([FromBody]Activity activity)
    {
        _logger.LogTrace($"Entered {nameof(GeneralInbox)} in {nameof(InboxController)}");

        if (!await VerifySignature(HttpContext.Request.Headers))
        {
            return BadRequest("Invalid Signature");
        }

        return Ok();
    }

    [HttpPost("{userId}")]
    public async Task<ActionResult> Log(Guid userId, [FromBody]Activity activity)
    {
        _logger.LogTrace($"Entered {nameof(Log)} in {nameof(InboxController)}");

        if (!await VerifySignature(HttpContext.Request.Headers))
        {
            return BadRequest("Invalid Signature");
        }


        return Ok();
    }

    private async Task<bool> VerifySignature(IHeaderDictionary requestHeaders)
    {
        _logger.LogTrace("Verifying Signature");

        var signatureHeader = requestHeaders["Signature"].First().Split(",").ToList();
        var keyId = new Uri(signatureHeader.FirstOrDefault(i => i.StartsWith("keyId"))?.Replace("keyId=", "")
            .Replace("\"", "") ?? string.Empty);
        var headers = signatureHeader.FirstOrDefault(i => i.StartsWith("headers"))?.Replace("headers=", "")
            .Replace("\"", "");
        var digest = signatureHeader.FirstOrDefault(i => i.StartsWith("digest"))?.Replace("digest=", "")
            .Replace("\"", "");
        var signatureHash = signatureHeader.FirstOrDefault(i => i.StartsWith("signature"))?.Replace("signature=", "")
            .Replace("\"", ""); // TODO Maybe converted to BASE 64
        _logger.LogDebug($"KeyId=\"{keyId}\"");

        var http = new HttpClient();
        var response = await http.GetAsync(keyId);
        if (response.IsSuccessStatusCode)
        {
            var resultActor = await response.Content.ReadFromJsonAsync<Actor>();
            
            var rsa = RSA.Create();
            rsa.ImportFromPem(resultActor.PublicKey.PublicKeyPem.ToCharArray());

            var comparisionString = $"(request-target): post /inbox\nhost: {requestHeaders.Host}\ndate: {requestHeaders.Date}\ndigest: {digest}";
            _logger.LogDebug($"{nameof(comparisionString)}=\"{comparisionString}\"");
            if (rsa.VerifyHash(Encoding.UTF8.GetBytes(signatureHash), Encoding.UTF8.GetBytes(comparisionString), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
            {
                _logger.LogDebug("Action with valid Signature received.");
                return true;
            }
            else
            {
                _logger.LogWarning("Action with invalid Signature received!!!");
                return false;
            }
        }
        else
        {
            _logger.LogInformation("Could not retrieve PublicKey");
            return false;
        }
    }
}