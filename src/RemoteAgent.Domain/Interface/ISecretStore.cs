
namespace RemoteAgent.Domain.Interface
{
    public interface ISecretStore
    {
        Task<bool> StoreKeyAsync(string Id, byte[] PublicKey, CancellationToken cancellationToken);
        Task<byte[]> TryGetKeyAsync(string Id, CancellationToken cancellationToken);
        Task<bool> DeleteKeyAsync(string Id, CancellationToken cancellationToken);
    }
}
