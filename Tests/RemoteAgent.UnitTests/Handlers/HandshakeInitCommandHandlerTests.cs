using Moq;
using RemoteAgent.Application.Handshake.Commands;
using RemoteAgent.Application.Handshake.Handlers;
using RemoteAgent.Domain.Interface;
using System.Security.Cryptography;

namespace RemoteAgent.UnitTests.Handlers
{
    public class HandshakeInitCommandHandlerTests
    {
        private readonly Mock<ISecretStore> _secretStoreMock;
        private readonly Mock<IEncryptionService> _encryptionServiceMock;
        private readonly HandshakeInitCommandHandler _handler;

        public HandshakeInitCommandHandlerTests()
        {
            _secretStoreMock = new Mock<ISecretStore>();
            _encryptionServiceMock = new Mock<IEncryptionService>();
            _handler = new HandshakeInitCommandHandler(_secretStoreMock.Object, _encryptionServiceMock.Object);
        }

        [Fact]
        public async Task Handle_WithValidCommand_ReturnsSuccess()
        {
            // Arrange
            var clientPublicKey = "valid-public-key";
            var sessionId = Guid.NewGuid().ToString();
            var command = new HandshakeInitCommand(clientPublicKey, sessionId);
            
            var serverPublicKey = new byte[] { 1, 2, 3, 4 };
            var sharedSecret = new byte[] { 5, 6, 7, 8 };

            _encryptionServiceMock
                .Setup(x => x.GetServerPublicKeyAndSharedSecret(clientPublicKey))
                .Returns((serverPublicKey, sharedSecret));
            
            _secretStoreMock
                .Setup(x => x.StoreKeyAsync(sessionId, sharedSecret, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Response);
            _encryptionServiceMock.Verify(x => x.GetServerPublicKeyAndSharedSecret(clientPublicKey), Times.Once);
            _secretStoreMock.Verify(x => x.StoreKeyAsync(sessionId, sharedSecret, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenKeyStorageFails_ReturnsFailure()
        {
            // Arrange
            var clientPublicKey = "valid-public-key";
            var sessionId = Guid.NewGuid().ToString();
            var command = new HandshakeInitCommand(clientPublicKey, sessionId);
            
            var serverPublicKey = new byte[] { 1, 2, 3, 4 };
            var sharedSecret = new byte[] { 5, 6, 7, 8 };

            _encryptionServiceMock
                .Setup(x => x.GetServerPublicKeyAndSharedSecret(clientPublicKey))
                .Returns((serverPublicKey, sharedSecret));
            
            _secretStoreMock
                .Setup(x => x.StoreKeyAsync(sessionId, sharedSecret, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Failed to store", result.Response?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task Handle_WhenKeyExchangeFails_ReturnsFailure()
        {
            // Arrange
            var clientPublicKey = "invalid-public-key";
            var sessionId = Guid.NewGuid().ToString();
            var command = new HandshakeInitCommand(clientPublicKey, sessionId);

            _encryptionServiceMock
                .Setup(x => x.GetServerPublicKeyAndSharedSecret(clientPublicKey))
                .Throws(new CryptographicException("Invalid key"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Key exchange failed", result.Response?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task Handle_WhenPublicKeyIsNull_ReturnsFailure()
        {
            // Arrange
            var clientPublicKey = "valid-public-key";
            var sessionId = Guid.NewGuid().ToString();
            var command = new HandshakeInitCommand(clientPublicKey, sessionId);

            _encryptionServiceMock
                .Setup(x => x.GetServerPublicKeyAndSharedSecret(clientPublicKey))
                .Returns((null!, null!));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Failed to generate shared secret", result.Response?.ToString() ?? string.Empty);
        }
    }
}
