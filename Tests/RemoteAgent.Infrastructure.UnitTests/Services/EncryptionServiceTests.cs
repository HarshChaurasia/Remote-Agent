using System.Security.Cryptography;
using RemoteAgent.Domain.Interface;
using RemoteAgent.Infrastructure.Security;
using Xunit;

namespace RemoteAgent.Infrastructure.UnitTests.Services
{
    public class EncryptionServiceTests
    {
        private readonly IEncryptionService _encryptionService;

        public EncryptionServiceTests()
        {
            _encryptionService = new EncryptionService();
        }

        [Fact]
        public void GetServerPublicKeyAndSharedSecret_WithValidClientPublicKey_ReturnsValidSharedSecret()
        {
            // Arrange
            var clientKeyPair = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            var clientPublicKeyBytes = clientKeyPair.PublicKey.ExportSubjectPublicKeyInfo();
            var clientPublicKey = Convert.ToBase64String(clientPublicKeyBytes);

            // Act
            var (serverPublicKey, sharedSecret) = _encryptionService.GetServerPublicKeyAndSharedSecret(clientPublicKey);

            // Assert
            Assert.NotNull(serverPublicKey);
            Assert.NotEmpty(serverPublicKey);
            Assert.NotNull(sharedSecret);
            Assert.Equal(32, sharedSecret.Length);
        }

        [Fact]
        public void Encrypt_WithValidInput_ReturnsEncryptedString()
        {
            // Arrange
            var plainText = "Hello, World!";
            var sharedSecret = GenerateRandomKey(32);

            // Act
            var encryptedText = _encryptionService.Encrypt(plainText, sharedSecret);

            // Assert
            Assert.NotEmpty(encryptedText);
            Assert.NotEqual(plainText, encryptedText);
        }

        [Fact]
        public void Decrypt_WithValidEncryption_ReturnsOriginalText()
        {
            // Arrange
            var plainText = "Hello, Secure World!";
            var sharedSecret = GenerateRandomKey(32);

            // Act
            var encryptedText = _encryptionService.Encrypt(plainText, sharedSecret);
            var decryptedText = _encryptionService.Decrypt(encryptedText, sharedSecret);

            // Assert
            Assert.Equal(plainText, decryptedText);
        }

        [Fact]
        public void Decrypt_WithTamperedCipherText_ThrowsCryptographicException()
        {
            // Arrange
            var plainText = "Secure message";
            var sharedSecret = GenerateRandomKey(32);

            var encryptedText = _encryptionService.Encrypt(plainText, sharedSecret);
            var encryptedBytes = Convert.FromBase64String(encryptedText);

            encryptedBytes[20] ^= 0xFF;
            var tamperedCipherText = Convert.ToBase64String(encryptedBytes);

            // Act & Assert
            Assert.Throws<CryptographicException>(() =>
                _encryptionService.Decrypt(tamperedCipherText, sharedSecret));
        }

        [Fact]
        public void Decrypt_WithWrongSharedSecret_ThrowsCryptographicException()
        {
            // Arrange
            var plainText = "Secure message";
            var sharedSecret = GenerateRandomKey(32);
            var wrongSecret = GenerateRandomKey(32);

            var encryptedText = _encryptionService.Encrypt(plainText, sharedSecret);

            // Act & Assert
            Assert.Throws<CryptographicException>(() =>
                _encryptionService.Decrypt(encryptedText, wrongSecret));
        }

        [Fact]
        public void KeyExchange_BothPartiesComputeIdenticalSharedSecret()
        {
            // Arrange
            var clientKeyPair = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            var serverEncryptionService = new EncryptionService();

            var clientPublicKeyBytes = clientKeyPair.PublicKey.ExportSubjectPublicKeyInfo();
            var clientPublicKey = Convert.ToBase64String(clientPublicKeyBytes);

            // Act
            var (serverPublicKeyBytes, serverSharedSecret) = serverEncryptionService.GetServerPublicKeyAndSharedSecret(clientPublicKey);

            using var serverEcdh = ECDiffieHellman.Create();
            serverEcdh.ImportSubjectPublicKeyInfo(serverPublicKeyBytes, out _);
            var clientSharedSecret = clientKeyPair.DeriveKeyMaterial(serverEcdh.PublicKey);

            // Assert
            Assert.Equal(serverSharedSecret, clientSharedSecret);
        }

        [Fact]
        public void EncryptingSameMessageTwice_ProducesDifferentCipherTexts()
        {
            // Arrange
            var plainText = "Test message";
            var sharedSecret = GenerateRandomKey(32);

            // Act
            var encrypted1 = _encryptionService.Encrypt(plainText, sharedSecret);
            var encrypted2 = _encryptionService.Encrypt(plainText, sharedSecret);

            // Assert
            Assert.NotEqual(encrypted1, encrypted2);

            Assert.Equal(plainText, _encryptionService.Decrypt(encrypted1, sharedSecret));
            Assert.Equal(plainText, _encryptionService.Decrypt(encrypted2, sharedSecret));
        }

        private byte[] GenerateRandomKey(int length)
        {
            var key = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            return key;
        }
    }
}
