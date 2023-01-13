using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CommonExtensions;
using Fedido.Server.Extensions;
using Fedido.Server.Interfaces;
using Fedido.Server.Model.ActivityPub;
using Fedido.Server.Model.Authentication;
using Fedido.Server.Model.Helpers;
using MongoDB.Driver;

namespace Fedido.Server.Handlers;

public class ActivityHandler : IActivityHandler
{
    private readonly ILogger<ActivityHandler> _logger;
    private readonly IMongoDbRepository _repository;

    public ActivityHandler(ILogger<ActivityHandler> logger, IMongoDbRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    public async Task<Actor> GetActor(Guid userId)
    {
        var filterActorDefinitionBuilder = Builders<Actor>.Filter;
        var filterActor = filterActorDefinitionBuilder.Eq(i => i.Id,
            new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/actor/{userId}"));
        var actor = await _repository.GetSpecificItem(filterActor, "ActivityPub", "Actors");
        return actor;
    }

    public async Task SendActivities(Activity activity, User user, Actor actor)
    {
        var targets = new List<ServerNameInboxPair>();

        var receivers = new List<string>();
        
        receivers.AddRange(activity.To);
        receivers.AddRange(activity.Bcc);
        receivers.AddRange(activity.Audience);
        receivers.AddRange(activity.Bto);
        receivers.AddRange(activity.Bcc);

        if (activity.IsActivityPublic()) // Public Post
        {
            foreach (var item in receivers)
            {
                // Check if item is a public string than skip
                
                var serverNameInboxPair = GetServerNameInboxPair(item);
                targets.Add(serverNameInboxPair);
            }
            
            // TODO Deliver to all known SharedInboxes
        }
        else // Private Post
        {
            foreach (var item in receivers)
            {
                var serverNameInboxPair = GetServerNameInboxPair(item);
                targets.Add(serverNameInboxPair);
            }
        }

        // TODO Remove duplicates from targets

        foreach (var target in targets) await SendActivity(activity, user, target, actor); // TODO Error Handling
    }

    private ServerNameInboxPair GetServerNameInboxPair(string item)
    {
        
        // Try to combine all actors to shared inboxes

        // TODO
        
        throw new NotImplementedException();
    }

    private async Task<bool> SendActivity(Activity activity, User user, ServerNameInboxPair serverInboxPair,
        Actor actor)
    {
        // Set Http Signature
        var jsonData = JsonSerializer.Serialize(activity);
        var digest = ComputeHash(jsonData);

        var rsa = RSA.Create();
        rsa.ImportFromPem(user.PrivateKeyActivityPub.ToCharArray());

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

        var httpResponse = await http.PostAsync(serverInboxPair.Inbox, contentData);

        if (httpResponse.IsSuccessStatusCode) return true;

        var responseText = await httpResponse.Content.ReadAsStringAsync();

        _logger.LogWarning($"An error occured sending an activity: {responseText}");

        return false;
    }

    private string ComputeHash(string jsonData)
    {
        var sha = SHA256.Create(); // Create a SHA256 hash from string   
        using var sha256Hash = SHA256.Create();
        // Computing Hash - returns here byte array
        var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(jsonData));

        var hashedString = Convert.ToBase64String(bytes);

        return hashedString;
    }
}