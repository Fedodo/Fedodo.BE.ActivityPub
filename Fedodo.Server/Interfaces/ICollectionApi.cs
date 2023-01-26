using Fedodo.Server.Model.ActivityPub;

namespace Fedodo.Server.Interfaces;

public interface ICollectionApi
{
    public Task<OrderedCollection<T>> GetOrderedCollection<T>(Uri orderedCollectionUri);
    public Task<Collection<T>> GetCollection<T>(Uri collectionUri);
}