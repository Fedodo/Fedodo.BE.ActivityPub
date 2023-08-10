namespace Fedodo.BE.ActivityPub.Interfaces.Repositories;

public interface INodeInfoRepository
{
    public Task<long> CountLocalPostsAsync();
    public Task<long> CountLocalActorsAsync();
}