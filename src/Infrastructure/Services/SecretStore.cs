using RemoteAgent.Domain.Interface;
using System.Collections.Concurrent;

namespace RemoteAgent.Infrastructure.Services
{
    /// <summary>
    /// Secret store service that store ECDH shared secrets used for encrypting/decrypting messages
    /// </summary>
    public class SecretStore : ISecretStore
    {
        private readonly ConcurrentDictionary<string, SharedSecretEntry> _sharedSecretDict = new();

        public Task<bool> StoreKeyAsync(string Id, byte[] sharedSecret, CancellationToken cancellationToken)
        {
            if(_sharedSecretDict.TryGetValue(Id, out _))
            {
                return Task.FromResult(false);
            }

            _sharedSecretDict[Id] = new SharedSecretEntry
            {
                SharedSecret = sharedSecret,
                StoredAt = DateTime.UtcNow
            };

            return Task.FromResult(true);
        }
        public Task<bool> DeleteKeyAsync(string Id, CancellationToken cancellationToken)
        {
            if (_sharedSecretDict.TryRemove(Id, out _))
            {
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task<byte[]> TryGetKeyAsync(string Id, CancellationToken cancellationToken)
        {
            if (_sharedSecretDict.TryGetValue(Id, out SharedSecretEntry? keyEntry))
            {
                if (DateTime.UtcNow - keyEntry.StoredAt > TimeSpan.FromHours(1))
                {
                    _sharedSecretDict.Remove(Id, out _);
                    return Task.FromResult<byte[]>(null!);
                }
                return Task.FromResult(keyEntry.SharedSecret);
            }
            return Task.FromResult<byte[]>(null!);
        }

        private class SharedSecretEntry
        {
            public required byte[] SharedSecret { get; set; }
            public DateTime StoredAt { get; set; }
        }
    }
}
