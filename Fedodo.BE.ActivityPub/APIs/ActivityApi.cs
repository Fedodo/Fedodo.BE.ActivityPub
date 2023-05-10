using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fedodo.BE.ActivityPub.Interfaces;
using Fedodo.BE.ActivityPub.Model.Helpers;
using Fedodo.NuGet.ActivityPub.Model.ActorTypes;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.Common.Models;

namespace Fedodo.BE.ActivityPub.APIs;

public class ActivityApi : IActivityAPI
{
    private readonly ILogger<ActivityApi> _logger;

    public ActivityApi(ILogger<ActivityApi> logger)
    {
        _logger = logger;
    }

    public async Task<bool> SendActivity(Activity activity, User user, ServerNameInboxPair serverInboxPair, Actor actor)
    {
        _logger.LogTrace($"Entered {nameof(SendActivity)} in {nameof(ActivityApi)}");

        // Set Http Signature
        var jsonData = JsonSerializer.Serialize(activity, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        var digest = ComputeHash(jsonData);

        var rsa = RSA.Create();
        rsa.ImportFromPem(user.PrivateKeyActivityPub!.ToCharArray());

        var date = DateTime.UtcNow.ToString("R");
        var signedString =
            $"(request-target): post {serverInboxPair.Inbox.AbsolutePath}\nhost: {serverInboxPair.ServerName}\ndate: {date}\ndigest: sha-256={digest}";
        var signature = rsa.SignData(Encoding.UTF8.GetBytes(signedString), HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        var signatureString = Convert.ToBase64String(signature);

        // Create HTTP request
        HttpClient http = new();
        http.DefaultRequestHeaders.Add("Host", serverInboxPair.ServerName);
        http.DefaultRequestHeaders.Add("Date", date);
        http.DefaultRequestHeaders.Add("Digest", $"sha-256={digest}");
        http.DefaultRequestHeaders.Add("Signature",
            $"keyId=\"{actor.PublicKey.Id}\",headers=\"(request-target) " +
            $"host date digest\",signature=\"{signatureString}\"");

        var contentData = new StringContent(jsonData, Encoding.UTF8, "application/ld+json");

        try
        {
            var httpResponse = await http.PostAsync(serverInboxPair.Inbox, contentData);

            if (httpResponse.IsSuccessStatusCode) return true;

            var responseText = await httpResponse.Content.ReadAsStringAsync();

            _logger.LogWarning($"An error occured sending an activity: {responseText}");
        }
        catch (HttpRequestException httpRequestException)
        {
            _logger.LogError(httpRequestException, "Sending an activity failed");
        }
        catch (TimeoutException httpRequestException)
        {
            _logger.LogError(httpRequestException, "Sending an activity timed out");
        }
        catch (TaskCanceledException httpRequestException)
        {
            _logger.LogError(httpRequestException, "Sending an activity was canceled");
        }

        return false;
    }

    public string ComputeHash(string jsonData)
    {
        var sha = SHA256.Create(); // Create a SHA256 hash from string   
        using var sha256Hash = SHA256.Create();
        // Computing Hash - returns here byte array
        var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(jsonData));

        var hashedString = Convert.ToBase64String(bytes);

        return hashedString;
    }
}