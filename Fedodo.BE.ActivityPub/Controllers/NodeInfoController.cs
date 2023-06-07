using Fedodo.BE.ActivityPub.Constants;
using Fedodo.BE.ActivityPub.Model;
using Fedodo.BE.ActivityPub.Model.NodeInfo;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using Fedodo.NuGet.Common.Models.Webfinger;
using Microsoft.AspNetCore.Mvc;

namespace Fedodo.BE.ActivityPub.Controllers;

[Produces("application/json")]
public class NodeInfoController : ControllerBase
{
    private readonly ILogger<NodeInfoController> _logger;
    private readonly IMongoDbRepository _repository;

    public NodeInfoController(ILogger<NodeInfoController> logger, IMongoDbRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [HttpGet(".well-known/nodeinfo")]
    public ActionResult<WebLink> GetNodeInfoLink()
    {
        _logger.LogTrace($"Entered {nameof(GetNodeInfoLink)} in {nameof(NodeInfoController)}");

        var wrapper = new
        {
            links = new List<NodeLink>
            {
                new()
                {
                    Rel = "http://nodeinfo.diaspora.software/ns/schema/2.1",
                    Href = new Uri($"https://{GeneralConstants.DomainName}/nodeinfo/2.1")
                }
            }
        };

        return Ok(wrapper);
    }

    [HttpGet("nodeinfo/2.1")]
    public async Task<ActionResult<NodeInfo>> GetNodeInfo2_1()
    {
        _logger.LogTrace($"Entered {nameof(GetNodeInfo2_1)} in {nameof(NodeInfoController)}");

        var version = Environment.GetEnvironmentVariable("VERSION") ?? "0.0.0";

        var nodeInfo = new NodeInfo
        {
            Version = "2.1",
            Software = new Software
            {
                Name = "Fedodo",
                Version = version,
                Repository = new Uri("https://github.com/Fedodo"),
                HomePage = new Uri("https://fedodo.org")
            },
            Protocols = new[]
            {
                "activitypub"
            },
            Services = new Services
            {
                Outbound = Array.Empty<object>(),
                Inbound = Array.Empty<object>()
            },
            Usage = new Usage
            {
                LocalPosts =
                    await _repository.CountAll<Activity>(DatabaseLocations.OutboxCreate.Database,
                        DatabaseLocations.OutboxCreate.Collection) + await _repository.CountAll<Activity>(
                        DatabaseLocations.OutboxAnnounce.Database, DatabaseLocations.OutboxAnnounce.Collection),
                LocalComments = 0, // TODO
                Users = new Users
                {
                    ActiveHalfyear = 1, // TODO
                    ActiveMonth = 1, // TODO
                    Total = await _repository.CountAll<Activity>(DatabaseLocations.Actors.Database,
                        DatabaseLocations.Actors.Collection)
                }
            },
            OpenRegistrations = true,
            Metadata = new Dictionary<string, string>()
        };

        return Ok(nodeInfo);
    }
}