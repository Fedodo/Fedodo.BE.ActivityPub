using System;
using System.Threading.Tasks;
using Fedido.Server.APIs;
using Fedido.Server.Handlers;
using Fedido.Server.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace Fedido.Server.Test.Handlers;

public class ActivityHandlerShould
{
    private readonly ActivityHandler _handler;

    public ActivityHandlerShould()
    {
        var logger = new Mock<ILogger<ActivityHandler>>();
        var repository = new Mock<IMongoDbRepository>();
        var actorApi = new Mock<IActorAPI>();
        var activityApi = new Mock<IActivityAPI>();
        var sharedInboxHandler = new Mock<IKnownSharedInboxHandler>();
        var collectionApi = new Mock<ICollectionApi>();
        
        _handler = new ActivityHandler(logger: logger.Object, repository: repository.Object, actorApi: actorApi.Object, 
            activityApi: activityApi.Object, sharedInboxHandler: sharedInboxHandler.Object, collectionApi: collectionApi.Object);
    }
    
    [Theory]
    [InlineData("00E90526-288D-41E2-9B21-39BAC05ED5B6", "lna-dev.net")]
    public async Task GetActor(string userId, string domainName)
    {
        // Arrange
        
        // Act
        var result = await _handler.GetActorAsync(new Guid(userId), domainName);

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public void SendActivities()
    {
        // Arrange
        
        // Act
        
        // Assert
    }
}