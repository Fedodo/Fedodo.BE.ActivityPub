using System.Security.Cryptography;
using System.Text;
using CommonExtensions;
using Fedido.Server.Interfaces;
using Fedido.Server.Model.ActivityPub;

namespace Fedido.Server.Handlers;

public class HttpSignatureHandler : IHttpSignatureHandler
{
    private readonly ILogger<HttpSignatureHandler> _logger;
    private readonly IActorAPI _actorApi;

    public HttpSignatureHandler(ILogger<HttpSignatureHandler> logger, IActorAPI actorApi)
    {
        _logger = logger;
        _actorApi = actorApi;
    }

    public async Task<bool> VerifySignature(IHeaderDictionary requestHeaders, string currentPath)
    {
        _logger.LogTrace("Verifying Signature");

        if (requestHeaders["Signature"].IsNullOrEmpty())
        {
            _logger.LogWarning($"Signature Header is NullOrEmpty in {nameof(VerifySignature)} in {nameof(HttpSignatureHandler)}");
            
            return false;
        }

        var signatureHeader = requestHeaders["Signature"].First().Split(",").ToList();

        foreach (var item in signatureHeader) _logger.LogDebug($"Signature Header Part=\"{item}\"");

        var keyIdString = signatureHeader.FirstOrDefault(i => i.StartsWith("keyId"))?.Replace("keyId=", "")
            .Replace("\"", "").Replace("#main-key", "");

        if (keyIdString.IsNullOrEmpty())
        {
            _logger.LogWarning($"{nameof(keyIdString)} is NullOrEmpty in {nameof(VerifySignature)} in {nameof(HttpSignatureHandler)}");

            return false;
        }
        
        var keyId = new Uri(keyIdString);
        var signatureHash = signatureHeader.FirstOrDefault(i => i.StartsWith("signature"))?.Replace("signature=", "")
            .Replace("\"", "");
        var headers = signatureHeader.FirstOrDefault(i => i.StartsWith("headers"))?.Replace("headers=", "")
            .Replace("\"", "");
        _logger.LogDebug($"KeyId=\"{keyId}\"");

        var response = await _actorApi.GetActor(keyId);
        if (response.IsNotNull())
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(response.PublicKey?.PublicKeyPem?.ToCharArray());

            string? comparisionString = null;

            switch (headers)
            {
                case "(request-target) host date digest":
                {
                    comparisionString =
                        $"(request-target): post {currentPath}\nhost: {requestHeaders.Host}\ndate: {requestHeaders.Date}\ndigest: {requestHeaders["Digest"]}"; // TODO Recompute Digest from Body TODO Validate Time
                    break;
                }
                case "(request-target) host date digest content-type":
                {
                    comparisionString =
                        $"(request-target): post {currentPath}\nhost: {requestHeaders.Host}\ndate: {requestHeaders.Date}\ndigest: {requestHeaders["Digest"]}\ncontent-type: {requestHeaders.ContentType}"; // TODO Recompute Digest from Body TODO Validate Time
                    break;
                }
                default:
                {
                    _logger.LogWarning($"No header configuration found for {headers}!");

                    return false;

                    break;
                }
            }

            _logger.LogDebug($"{nameof(comparisionString)}=\"{comparisionString}\"");

            if (rsa.VerifyData(Encoding.UTF8.GetBytes(comparisionString), Convert.FromBase64String(signatureHash),
                    HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
            {
                _logger.LogDebug("Action with valid Signature received.");
                return true;
            }

            _logger.LogWarning("Action with invalid Signature received!!!");
            return false;
        }

        _logger.LogInformation("Could not retrieve PublicKey");
        return false;
    }
}