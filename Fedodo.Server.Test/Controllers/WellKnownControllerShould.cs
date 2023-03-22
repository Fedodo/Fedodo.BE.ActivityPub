using System.Threading.Tasks;
using CommonExtensions;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using Fedodo.Server.Controllers;
using Fedodo.Server.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using Shouldly;
using Xunit;

namespace Fedodo.Server.Test.Controllers;

public class WellKnownControllerShould
{
    private readonly Webfinger _webfinger;
    private readonly WellKnownController _wellKnownController;

    public WellKnownControllerShould()
    {
        var logger = new Mock<ILogger<WellKnownController>>();
        var repository = new Mock<IMongoDbRepository>();

        var filterDefinitionBuilder = Builders<Webfinger>.Filter;
        var filter = filterDefinitionBuilder.Eq(i => i.Subject, "resource");

        _webfinger = new Webfinger
        {
            Subject = "resource",
            Links = new[]
            {
                new WebLink()
            }
        };

        repository.Setup(i =>
                i.GetSpecificItem(It.Is<FilterDefinition<Webfinger>>(i =>
                    i.IsSameAs(filter)), DatabaseLocations.Webfinger.Database, DatabaseLocations.Webfinger.Collection))
            .ReturnsAsync(_webfinger);

        _wellKnownController = new WellKnownController(logger.Object, repository.Object);
    }

    [Theory]
    [InlineData("resource")]
    public async Task GetWebfinger(string resource)
    {
        // Arrange

        // Act
        var result = await _wellKnownController.GetWebfingerAsync(resource);

        // Assert
        result.ShouldNotBeNull();
        result.Result.ShouldBeOfType<OkObjectResult>();
    }
}