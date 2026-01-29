using RemoteAgent.Domain.Interface;
using RemoteAgent.Infrastructure.Services;
using Xunit;

namespace RemoteAgent.Infrastructure.UnitTests.Services
{
    public class SecretStoreTests
    {
        private readonly ISecretStore _secretStore;

        public SecretStoreTests()
        {
            _secretStore = new SecretStore();
        }

        [Fact]
        public async Task StoreKeyAsync_WithValidKey_ReturnsTrue()
        {
            // Arrange
            var sessionId = Guid.NewGuid().ToString();
            var sharedSecret = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };

            // Act
            var result = await _secretStore.StoreKeyAsync(sessionId, sharedSecret, CancellationToken.None);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task StoreKeyAsync_ThenTryGetKeyAsync_ReturnsSameKey()
        {
            // Arrange
            var sessionId = Guid.NewGuid().ToString();
            var sharedSecret = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };

            // Act
            await _secretStore.StoreKeyAsync(sessionId, sharedSecret, CancellationToken.None);
            var retrievedKey = await _secretStore.TryGetKeyAsync(sessionId, CancellationToken.None);

            // Assert
            Assert.NotNull(retrievedKey);
            Assert.Equal(sharedSecret, retrievedKey);
        }

        [Fact]
        public async Task TryGetKeyAsync_WithNonExistentKey_ReturnsNull()
        {
            // Act
            var result = await _secretStore.TryGetKeyAsync(Guid.NewGuid().ToString(), CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteKeyAsync_WithExistingKey_ReturnsTrue()
        {
            // Arrange
            var sessionId = Guid.NewGuid().ToString();
            var sharedSecret = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            await _secretStore.StoreKeyAsync(sessionId, sharedSecret, CancellationToken.None);

            // Act
            var result = await _secretStore.DeleteKeyAsync(sessionId, CancellationToken.None);

            // Assert
            Assert.True(result);

            var retrievedKey = await _secretStore.TryGetKeyAsync(sessionId, CancellationToken.None);
            Assert.Null(retrievedKey);
        }

        [Fact]
        public async Task DeleteKeyAsync_WithNonExistentKey_ReturnsFalse()
        {
            // Act
            var result = await _secretStore.DeleteKeyAsync(Guid.NewGuid().ToString(), CancellationToken.None);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task StoreKeyAsync_WithMultipleSessions_StoresAllKeys()
        {
            // Arrange
            var session1 = Guid.NewGuid().ToString();
            var session2 = Guid.NewGuid().ToString();
            var secret1 = new byte[] { 1, 2, 3, 4 };
            var secret2 = new byte[] { 5, 6, 7, 8 };

            // Act
            await _secretStore.StoreKeyAsync(session1, secret1, CancellationToken.None);
            await _secretStore.StoreKeyAsync(session2, secret2, CancellationToken.None);

            var retrieved1 = await _secretStore.TryGetKeyAsync(session1, CancellationToken.None);
            var retrieved2 = await _secretStore.TryGetKeyAsync(session2, CancellationToken.None);

            // Assert
            Assert.NotNull(retrieved1);
            Assert.NotNull(retrieved2);
            Assert.Equal(secret1, retrieved1);
            Assert.Equal(secret2, retrieved2);
        }

        [Fact]
        public async Task StoreKeyAsync_WithSameSessionId_ReturnsFalse()
        {
            // Arrange
            var sessionId = Guid.NewGuid().ToString();
            var secret1 = new byte[] { 1, 2, 3, 4 };
            var secret2 = new byte[] { 5, 6, 7, 8 };

            // Act
            var result1 = await _secretStore.StoreKeyAsync(sessionId, secret1, CancellationToken.None);
            var result2 = await _secretStore.StoreKeyAsync(sessionId, secret2, CancellationToken.None);

            var retrieved = await _secretStore.TryGetKeyAsync(sessionId, CancellationToken.None);

            // Assert
            Assert.True(result1);
            Assert.False(result2);
            Assert.NotNull(retrieved);
            Assert.Equal(secret1, retrieved);
        }
    }
}
