
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;

namespace Fedodo.BE.ActivityPub.Interfaces;

public interface ICollectionApi
{
    public Task<OrderedCollection> GetOrderedCollection<T>(Uri orderedCollectionUri);
    public Task<Collection> GetCollection<T>(Uri collectionUri);
}