using RemoteAgent.Application.Handshake.Commands;
using RemoteAgent.Domain.Common;
using RemoteAgent.Domain.Interface;
using System.Security.Cryptography;

namespace RemoteAgent.Application.Handshake.Handlers
{
    public class HandshakeInitCommandHandler : ICommandHandler<HandshakeInitCommand>
    {
        private readonly ISecretStore _publicKeyStore;
        private readonly IEncryptionService _encryptionService;
        
        public HandshakeInitCommandHandler(
            ISecretStore publicKeyStore, 
            IEncryptionService encryptionService)
        {
            _publicKeyStore = publicKeyStore;
            _encryptionService = encryptionService;
        }
        
        public async Task<HandlerResponse> Handle(HandshakeInitCommand command, CancellationToken cancellationToken)
        {
            try
            {
                var (serverPublicKey, sharedSecret) = _encryptionService.GetServerPublicKeyAndSharedSecret(command.PublicKey);
                if (serverPublicKey == null || sharedSecret == null)
                {
                    return new HandlerResponse("Failed to generate shared secret", false);
                }
                    
                var isKeyStored = await _publicKeyStore.StoreKeyAsync(command.SessionId, sharedSecret, cancellationToken);
                if (!isKeyStored)
                {
                    return new HandlerResponse("Failed to store shared secret", false);
                }
                    
                return new HandlerResponse(Convert.ToBase64String(serverPublicKey), true);
            }
            catch (CryptographicException ex)
            {
                return new HandlerResponse($"Key exchange failed: {ex.Message}", false);
            }
            catch (Exception ex)
            {
                return new HandlerResponse($"Handshake failed: {ex.Message}", false);
            }
        }
    }
}
