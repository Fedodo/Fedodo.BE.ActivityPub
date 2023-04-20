using System.Collections.ObjectModel;
using Fedodo.BE.ActivityPub.Interfaces;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;

namespace Fedodo.BE.ActivityPub.APIs;

public class CollectionApi : ICollectionApi
{
    private readonly ILogger<CollectionApi> _logger;

    public CollectionApi(ILogger<CollectionApi> logger)
    {
        _logger = logger;
    }

    public async Task<OrderedCollection> GetOrderedCollection<T>(Uri orderedCollectionUri)
    {
        HttpClient http = new();
        http.DefaultRequestHeaders.Add("Accept", "application/ld+json");

        var httpResponse = await http.GetAsync(orderedCollectionUri);

        if (httpResponse.IsSuccessStatusCode)
        {
            var collection = await httpResponse.Content.ReadFromJsonAsync<OrderedCollection>();

            return collection;
        }

        var responseText = await httpResponse.Content.ReadAsStringAsync();

        _logger.LogWarning($"An error occured getting an {nameof(orderedCollectionUri)}: {responseText}");

        return null;
    }

    public async Task<Collection> GetCollection<T>(Uri collectionUri)
    {
        HttpClient http = new();
        http.DefaultRequestHeaders.Add("Accept", "application/ld+json");

        var httpResponse = await http.GetAsync(collectionUri);

        if (httpResponse.IsSuccessStatusCode)
        {
            var collection = await httpResponse.Content.ReadFromJsonAsync<Collection>();

            return collection;
        }

        var responseText = await httpResponse.Content.ReadAsStringAsync();

        _logger.LogWarning($"An error occured getting an {nameof(collectionUri)}: {responseText}");

        return null;
    }
}