using Fedodo.BE.ActivityPub.Model;
using Fedodo.BE.ActivityPub.Model.NodeInfo;
using Fedodo.NuGet.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fedodo.BE.ActivityPub.Controllers;

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
                    Rel = "http://nodeinfo.diaspora.software/ns/schema/2.0",
                    Href = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/nodeinfo/2.0")
                }
            }
        };

        return Ok(wrapper);
    }

    [HttpGet("nodeinfo/2.0")]
    public ActionResult<NodeInfo> GetNodeInfo()
    {
        _logger.LogTrace($"Entered {nameof(GetNodeInfo)} in {nameof(NodeInfoController)}");

        var nodeInfo = new NodeInfo
        {
            Version = "2.0",
            Software = new Software
            {
                Name = "Fedodo",
                Version = "0.1"
            },
            Protocols = new[]
            {
                "activitypub"
            },
            Services = new Services
            {
                Outbound = new object[0],
                Inbound = new object[0]
            },
            Usage = new Usage
            {
                LocalPosts = 0, // TODO
                LocalComments = 0, // TODO
                Users = new Users
                {
                    ActiveHalfyear = 1, // TODO
                    ActiveMonth = 1, // TODO
                    Total = 1 // TODO
                }
            },
            OpenRegistrations = false,
            Metadata = new Dictionary<string, string>()
        };

        return Ok(nodeInfo);
    }
}