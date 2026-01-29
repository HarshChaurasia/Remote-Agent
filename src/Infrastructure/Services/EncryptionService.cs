using RemoteAgent.Domain.Interface;
using System.Security.Cryptography;
using System.Text;

namespace RemoteAgent.Infrastructure.Security
{
    /// <summary>
    /// Handles encryption and decryption
    /// </summary>
    public class EncryptionService : IEncryptionService
    {
        private const int IvSize = 16; // 128 bits
        private const int HmacSize = 32; // 256 bits
        
        private readonly ECDiffieHellman _serverKeyPair;
        
        public EncryptionService()
        {     
            
            // Here, server key is generated everytime, in prod it should be loaded from a secure storage.
            _serverKeyPair = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        }

        public (byte[], byte[]) GetServerPublicKeyAndSharedSecret(string clientPublicKey)
        {
            try
            {
                var clientPublicKeyBytes = Convert.FromBase64String(clientPublicKey);

                using var clientEcdh = ECDiffieHellman.Create();
                clientEcdh.ImportSubjectPublicKeyInfo(clientPublicKeyBytes, out _);

                var sharedSecret = _serverKeyPair.DeriveKeyMaterial(clientEcdh.PublicKey);

                var serverPublicKeyBytes = _serverKeyPair.PublicKey.ExportSubjectPublicKeyInfo();
                return (serverPublicKeyBytes, sharedSecret);
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Key exchange initialization failed", ex.Message);
            }
        }

        public string Encrypt(string plainText, byte[] sharedSecret)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentException("Plain text cannot be null or empty", nameof(plainText));
            if (sharedSecret == null || sharedSecret.Length == 0)
                throw new ArgumentException("Shared secret cannot be null or empty", nameof(sharedSecret));

            // deriving encryption key and HMAC key from shared secret
            var (encryptionKey, hmacKey) = DeriveKeys(sharedSecret);

            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = encryptionKey;
            aes.GenerateIV(); // here using Random IV for better security

            byte[] encrypted;
            using (var encryptor = aes.CreateEncryptor())
            {
                var plainBytes = Encoding.UTF8.GetBytes(plainText);
                encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            }

            using var hmac = new HMACSHA256(hmacKey);
            var hmacBytes = hmac.ComputeHash(encrypted);

            // Combinining IV + Encrypted data + HMAC and same logic will be used for decryption also
            var result = new byte[aes.IV.Length + encrypted.Length + hmacBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(encrypted, 0, result, aes.IV.Length, encrypted.Length);
            Buffer.BlockCopy(hmacBytes, 0, result, aes.IV.Length + encrypted.Length, hmacBytes.Length);

            return Convert.ToBase64String(result);
        }

        public string Decrypt(string cipherText, byte[] sharedSecret)
        {
            if (string.IsNullOrEmpty(cipherText))
                throw new ArgumentException("Cipher text cannot be null or empty", nameof(cipherText));
            if (sharedSecret == null || sharedSecret.Length == 0)
                throw new ArgumentException("Shared secret cannot be null or empty", nameof(sharedSecret));

            try
            {
                var cipherBytes = Convert.FromBase64String(cipherText);

                if (cipherBytes.Length < IvSize + HmacSize)
                    throw new CryptographicException("Invalid cipher text length");

                // Extract iv, encrypted data, and HMAC based on enryption logic
                var iv = new byte[IvSize];
                var encrypted = new byte[cipherBytes.Length - IvSize - HmacSize];
                var receivedHmac = new byte[HmacSize];

                Buffer.BlockCopy(cipherBytes, 0, iv, 0, IvSize);
                Buffer.BlockCopy(cipherBytes, IvSize, encrypted, 0, encrypted.Length);
                Buffer.BlockCopy(cipherBytes, IvSize + encrypted.Length, receivedHmac, 0, HmacSize);

                var (encryptionKey, hmacKey) = DeriveKeys(sharedSecret);

                using var hmac = new HMACSHA256(hmacKey);
                var computedHmac = hmac.ComputeHash(encrypted);

                // Using constant-time comparison to prevent timing attacks
                if (!CryptographicOperations.FixedTimeEquals(computedHmac, receivedHmac))
                    throw new CryptographicException("HMAC verification failed - data may be tampered");

                using var aes = Aes.Create();
                aes.KeySize = 256;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = encryptionKey;
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                var decrypted = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);

                return Encoding.UTF8.GetString(decrypted);
            }
            catch (Exception ex) when (ex is FormatException || ex is CryptographicException)
            {
                throw new CryptographicException("Decryption failed", ex);
            }
        }

        /// <summary>
        /// Using HMAC-based approach to derive two different keys HKDF (HMAC-based Key Derivation Function)
        /// </summary>
        private (byte[] encryptionKey, byte[] hmacKey) DeriveKeys(byte[] sharedSecret)
        {
            using var hmac = new HMACSHA256(sharedSecret);

            var encryptionKeyInfo = Encoding.UTF8.GetBytes("EncryptionKey");
            var encryptionKey = hmac.ComputeHash(encryptionKeyInfo);

            var hmacKeyInfo = Encoding.UTF8.GetBytes("HMACKey");
            var hmacKey = hmac.ComputeHash(hmacKeyInfo);

            return (encryptionKey, hmacKey);
        }
    }
}
