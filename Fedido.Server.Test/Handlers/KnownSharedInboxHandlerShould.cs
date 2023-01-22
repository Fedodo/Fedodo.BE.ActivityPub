using Fedido.Server.Handlers;
using Fedido.Server.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fedido.Server.Test.Handlers;

public class KnownSharedInboxHandlerShould
{
    private readonly KnownSharedInboxHandler _handler;

    public KnownSharedInboxHandlerShould()
    {
        var logger = new Mock<ILogger<KnownSharedInboxHandler>>();
        var repository = new Mock<IMongoDbRepository>();
        
        _handler = new KnownSharedInboxHandler(logger.Object, repository.Object);
    }
    
    [Fact]
    public void AddSharedInbox()
    {
        // Arrange
        
        // Act
        
        // Assert
    }
}