using Fedodo.Server.Interfaces;
using Fedodo.Server.Model.ActivityPub;

namespace Fedodo.Server.APIs;

public class CollectionApi : ICollectionApi
{
    private readonly ILogger<CollectionApi> _logger;

    public CollectionApi(ILogger<CollectionApi> logger)
    {
        _logger = logger;
    }

    public async Task<OrderedCollection<T>> GetOrderedCollection<T>(Uri orderedCollectionUri)
    {
        HttpClient http = new();
        http.DefaultRequestHeaders.Add("Accept", "application/ld+json");

        var httpResponse = await http.GetAsync(orderedCollectionUri);

        if (httpResponse.IsSuccessStatusCode)
        {
            var collection = await httpResponse.Content.ReadFromJsonAsync<OrderedCollection<T>>();

            return collection;
        }

        var responseText = await httpResponse.Content.ReadAsStringAsync();

        _logger.LogWarning($"An error occured getting an {nameof(orderedCollectionUri)}: {responseText}");

        return null;
    }

    public async Task<Collection<T>> GetCollection<T>(Uri collectionUri)
    {
        HttpClient http = new();
        http.DefaultRequestHeaders.Add("Accept", "application/ld+json");

        var httpResponse = await http.GetAsync(collectionUri);

        if (httpResponse.IsSuccessStatusCode)
        {
            var collection = await httpResponse.Content.ReadFromJsonAsync<Collection<T>>();

            return collection;
        }

        var responseText = await httpResponse.Content.ReadAsStringAsync();

        _logger.LogWarning($"An error occured getting an {nameof(collectionUri)}: {responseText}");

        return null;
    }
}