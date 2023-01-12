using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Fedido.Server.Extensions;
using CommonExtensions;
using Fedido.Server.Interfaces;
using Fedido.Server.Model.ActivityPub;
using Fedido.Server.Model.Authentication;
using Fedido.Server.Model.Helpers;
using MongoDB.Driver;

namespace Fedido.Server.Handlers;

public class ActivityHandler : IActivityHandler
{
    private readonly IKnownServersHandler _knownServersHandler;
    private readonly ILogger<ActivityHandler> _logger;
    private readonly IMongoDbRepository _repository;

    public ActivityHandler(ILogger<ActivityHandler> logger, IKnownServersHandler knownServersHandler,
        IMongoDbRepository repository)
    {
        _logger = logger;
        _knownServersHandler = knownServersHandler;
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

        if (activity.IsActivityPublic() && activity.Type == "Create")
        {
            var post = activity.ExtractItemFromObject<Post>();

            if (post.InReplyTo.IsNotNull()) await _knownServersHandler.Add(post.InReplyTo.ToString());

            var servers = await _knownServersHandler.GetAll();

            foreach (var item in servers)
                targets.Add(new ServerNameInboxPair
                {
                    ServerName = item.ServerDomainName,
                    Inbox = item.DefaultInbox
                });
        }
        else if (activity.IsActivityPublic())
        {
            var servers = await _knownServersHandler.GetAll();

            foreach (var item in servers)
                targets.Add(new ServerNameInboxPair
                {
                    ServerName = item.ServerDomainName,
                    Inbox = item.DefaultInbox
                });
        }
        else
        {
            foreach (var item in activity.To)
            {
                var serverNameInboxPair = new ServerNameInboxPair
                {
                    ServerName = item.ExtractServerName(),
                    Inbox = new Uri(item)
                };

                targets.Add(serverNameInboxPair);

                await _knownServersHandler.Add(item);
            }
        }

        foreach (var target in targets) await SendActivity(activity, user, target, actor); // TODO Error Handling
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