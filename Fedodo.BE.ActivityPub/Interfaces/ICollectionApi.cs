using Fedodo.NuGet.ActivityPub.Model;

namespace Fedodo.BE.ActivityPub.Interfaces;

public interface ICollectionApi
{
    public Task<OrderedCollection<T>> GetOrderedCollection<T>(Uri orderedCollectionUri);
    public Task<Collection<T>> GetCollection<T>(Uri collectionUri);
}