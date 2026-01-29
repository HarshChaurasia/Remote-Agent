namespace RemoteAgent.Domain.Interface
{
    public interface IEncryptionService
    {

        (byte[], byte[]) GetServerPublicKeyAndSharedSecret(string clientPublicKey); 
        string Encrypt(string plainText, byte[] sharedSecret);
        string Decrypt(string cipherText, byte[] sharedSecret);
    }
}
