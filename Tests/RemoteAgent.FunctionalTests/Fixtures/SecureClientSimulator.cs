using System.Security.Cryptography;
using System.Text;
using RemoteAgent.Domain.Interface;
using RemoteAgent.Infrastructure.Security;

namespace RemoteAgent.FunctionalTests.Fixtures
{
    /// <summary>
    /// Client simulation for ECDH key exchange and encryption/decryption.
    /// </summary>
    public class Client
    {
        private readonly ECDiffieHellman _clientKeyPair;
        private byte[]? _sharedSecret;
        public string SessionId { get; private set; } = string.Empty;
        public IEncryptionService EncryptionService { get; }

        public Client()
        {
            _clientKeyPair = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            
            EncryptionService = new EncryptionService();
        }


        public string GetClientPublicKey()
        {
            var publicKeyBytes = _clientKeyPair.PublicKey.ExportSubjectPublicKeyInfo();
            return Convert.ToBase64String(publicKeyBytes);
        }

        public void CompleteHandshake(byte[] serverPublicKeyBytes, string sessionId)
        {
            using var serverEcdh = ECDiffieHellman.Create();
            serverEcdh.ImportSubjectPublicKeyInfo(serverPublicKeyBytes, out _);

            // deriving client side shared secret using server public key
            _sharedSecret = _clientKeyPair.DeriveKeyMaterial(serverEcdh.PublicKey);
            SessionId = sessionId;
        }

        public string EncryptMessage(string plainText)
        {
            if (_sharedSecret == null)
                throw new InvalidOperationException("Handshake must be completed first");

            return EncryptionService.Encrypt(plainText, _sharedSecret);
        }

        public string DecryptMessage(string cipherText)
        {
            if (_sharedSecret == null)
                throw new InvalidOperationException("Handshake must be completed first");

            return EncryptionService.Decrypt(cipherText, _sharedSecret);
        }

        public byte[]? GetSharedSecret() => _sharedSecret;
    }
}
