using Fedodo.BE.ActivityPub.Controllers;
using Fedodo.NuGet.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace Fedodo.BE.ActivityPub.Test.Controllers;

public class NodeInfoControllerShould
{
    private readonly NodeInfoController _nodeInfoController;

    public NodeInfoControllerShould()
    {
        var logger = new Mock<ILogger<NodeInfoController>>();
        var repository = new Mock<IMongoDbRepository>();

        _nodeInfoController = new NodeInfoController(logger.Object, repository.Object);
    }

    // [Fact]
    // public void GetNodeInfoLink()
    // {
    //     // Arrange
    //     
    //     // Act
    //     var result = _nodeInfoController.GetNodeInfoLink();
    //
    //     // Assert
    //     result.Result.ShouldBeOfType<OkObjectResult>();
    // }
}