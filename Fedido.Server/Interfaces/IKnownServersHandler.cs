using Fedido.Server.Model;

namespace Fedido.Server.Interfaces;

public interface IKnownServersHandler
{
    public Task<IEnumerable<ActivityPubServer>> GetAll();
    public Task Add(string? postTo);
}