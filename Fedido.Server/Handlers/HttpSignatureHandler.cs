using System.Security.Cryptography;
using System.Text;
using Fedido.Server.Interfaces;
using Fedido.Server.Model.ActivityPub;

namespace Fedido.Server.Handlers;

public class HttpSignatureHandler : IHttpSignatureHandler
{
    private readonly ILogger<HttpSignatureHandler> _logger;

    public HttpSignatureHandler(ILogger<HttpSignatureHandler> logger)
    {
        _logger = logger;
    }

    public async Task<bool> VerifySignature(IHeaderDictionary requestHeaders, string currentPath)
    {
        _logger.LogTrace("Verifying Signature");

        var signatureHeader = requestHeaders["Signature"].First().Split(",").ToList();

        foreach (var item in signatureHeader) _logger.LogDebug($"Signature Header Part=\"{item}\"");

        var keyId = new Uri(signatureHeader.FirstOrDefault(i => i.StartsWith("keyId"))?.Replace("keyId=", "")
            .Replace("\"", "").Replace("#main-key", "") ?? string.Empty);
        var signatureHash = signatureHeader.FirstOrDefault(i => i.StartsWith("signature"))?.Replace("signature=", "")
            .Replace("\"", "");
        var headers = signatureHeader.FirstOrDefault(i => i.StartsWith("headers"))?.Replace("headers=", "")
            .Replace("\"", "");
        _logger.LogDebug($"KeyId=\"{keyId}\"");

        var http = new HttpClient();
        http.DefaultRequestHeaders.Add("Accept", "application/ld+json");
        var response = await http.GetAsync(keyId);
        if (response.IsSuccessStatusCode)
        {
            var resultActor = await response.Content.ReadFromJsonAsync<Actor>();

            var rsa = RSA.Create();
            rsa.ImportFromPem(resultActor.PublicKey.PublicKeyPem.ToCharArray());

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