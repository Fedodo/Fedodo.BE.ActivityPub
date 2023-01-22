using System;
using System.Collections.Generic;
using Fedido.Server.Handlers;
using Fedido.Server.Interfaces;
using Fedido.Server.Model.Helpers;
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

        var sharedInboxes = new List<SharedInbox>()
        {
            new()
            {
                Id = new Guid("3F155F70-A64B-4F7E-A410-F6E7E861C9FA"),
                SharedInboxUri = new Uri("https://example.com/inbox")
            },
            new()
            {
                Id = new Guid("3EBBB5F7-E9F0-475A-87FA-D3EA93408345"),
                SharedInboxUri = new Uri("https://example.com/inbox")
            }
        };

        // repository.Setup(i => i.GetSpecificItems())

        repository.Setup(i => i.GetAll<SharedInbox>(DatabaseLocations.KnownSharedInbox.Database,
            DatabaseLocations.KnownSharedInbox.Collection)).ReturnsAsync(sharedInboxes);

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