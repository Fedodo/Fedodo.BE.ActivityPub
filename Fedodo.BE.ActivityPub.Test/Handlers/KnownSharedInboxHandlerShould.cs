using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fedodo.BE.ActivityPub.Interfaces.APIs;
using Fedodo.BE.ActivityPub.Model.Helpers;
using Fedodo.BE.ActivityPub.Services;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace Fedodo.BE.ActivityPub.Test.Handlers;

public class KnownSharedInboxHandlerShould
{
    private readonly KnownSharedInboxService _service;
    private readonly List<SharedInbox> _sharedInboxes;

    public KnownSharedInboxHandlerShould()
    {
        var logger = new Mock<ILogger<KnownSharedInboxService>>();
        var repository = new Mock<IMongoDbRepository>();
        var actorApi = new Mock<IActorAPI>();

        _sharedInboxes = new List<SharedInbox>
        {
            new()
            {
                Id = new Guid("3F155F70-A64B-4F7E-A410-F6E7E861C9FA"),
                SharedInboxUri = new Uri("https://example.com/inbox")
            },
            new()
            {
                Id = new Guid("3EBBB5F7-E9F0-475A-87FA-D3EA93408345"),
                SharedInboxUri = new Uri("https://example.com/inbox2")
            }
        };

        // repository.Setup(i => i.GetSpecificItems())

        repository.Setup(i => i.GetAll<SharedInbox>(DatabaseLocations.KnownSharedInbox.Database,
            DatabaseLocations.KnownSharedInbox.Collection)).ReturnsAsync(_sharedInboxes);

        _service = new KnownSharedInboxService(logger.Object, repository.Object, actorApi.Object);
    }

    [Theory]
    [InlineData("https://lna-dev.net")]
    public async Task AddSharedInbox(string sharedInbox)
    {
        // Arrange

        // Act
        await _service.AddSharedInboxAsync(new Uri(sharedInbox));

        // Assert
    }

    [Fact]
    public async Task GetSharedInboxes()
    {
        // Arrange

        // Act
        var result = await _service.GetSharedInboxesAsync();

        // Assert
        result.ShouldNotBeNull();
        result.First().ShouldBe(new Uri("https://example.com/inbox"));
    }
}