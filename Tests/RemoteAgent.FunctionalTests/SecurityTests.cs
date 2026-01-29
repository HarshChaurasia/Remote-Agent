using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using RemoteAgent.WebAPI;
using RemoteAgent.WebAPI.Model;
using RemoteAgent.FunctionalTests.Fixtures;

namespace RemoteAgent.FunctionalTests
{

    /// <summary>
    /// Functional tests for validating security related scenarios of remote agent.
    /// </summary>
    public class SecurityTests : IAsyncLifetime
    {
        private WebApplicationFactory<Program>? _factory;
        private HttpClient? _httpClient;
        private Client? _client;

        public async Task InitializeAsync()
        {
            _factory = new WebApplicationFactory<Program>();
            _httpClient = _factory.CreateClient();
            _client = new Client();
            
            await PerformHandshakeAsync();
        }

        public async Task DisposeAsync()
        {
            _httpClient?.Dispose();
            _factory?.Dispose();
        }

        private async Task PerformHandshakeAsync()
        {
            if (_httpClient == null || _client == null)
                throw new InvalidOperationException("Not initialized");

            var publicKey = _client.GetClientPublicKey();
            var request = new HandshakeRequest(publicKey);

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("/handshake/init", jsonContent);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var handshakeResponse = JsonSerializer.Deserialize<HandshakeResponse>(responseBody, options);

            if (handshakeResponse == null || string.IsNullOrEmpty(handshakeResponse.PublicKey))
                throw new InvalidOperationException("Invalid handshake response");

            var serverPublicKeyBytes = Convert.FromBase64String(handshakeResponse.PublicKey);
            _client.CompleteHandshake(serverPublicKeyBytes, handshakeResponse.sessionId);
        }



        [Fact]
        public async Task UnencryptedRequest_ShouldBeRejected()
        {
            // Arrange
            if (_httpClient == null)
                throw new InvalidOperationException("Not initialized");

            // Act 
            var response = await _httpClient.GetAsync("/plugins");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task InvalidFileUpload_EmptyFile_ShouldFail()
        {
            // Arrange
            if (_httpClient == null || _client == null)
                throw new InvalidOperationException("Not initialized");

            var uploadPayload = new
            {
                dllFile = Convert.ToBase64String(Array.Empty<byte>()),
                name = "EmptyPlugin"
            };

            var encryptedUpload = _client.EncryptMessage(JsonSerializer.Serialize(uploadPayload));
            var request = new HttpRequestMessage(HttpMethod.Post, "/plugins")
            {
                Content = new StringContent(encryptedUpload, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-Session-Id", _client.SessionId);

            // Act
            var response = await _httpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }


        [Fact]
        public async Task InvalidFileUpload_MissingName_ShouldFail()
        {
            // Arrange
            if (_httpClient == null || _client == null)
                throw new InvalidOperationException("Not initialized");

            var uploadPayload = new
            {
                dllFile = Convert.ToBase64String(new byte[] { 1, 2, 3 })
            };

            var encryptedUpload = _client.EncryptMessage(JsonSerializer.Serialize(uploadPayload));
            var request = new HttpRequestMessage(HttpMethod.Post, "/plugins")
            {
                Content = new StringContent(encryptedUpload, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-Session-Id", _client.SessionId);

            // Act
            var response = await _httpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }


        [Fact]
        public async Task InvalidFileUpload_WrongFileExtension_ShouldFail()
        {
            // Arrange
            if (_httpClient == null || _client == null)
                throw new InvalidOperationException("Not initialized");

            var uploadPayload = new
            {
                dllFile = Convert.ToBase64String(new byte[] { 1, 2, 3 }),
                name = "BadPlugin"
            };

            var encryptedUpload = _client.EncryptMessage(JsonSerializer.Serialize(uploadPayload));
            var request = new HttpRequestMessage(HttpMethod.Post, "/plugins")
            {
                Content = new StringContent(encryptedUpload, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-Session-Id", _client.SessionId);

            // Act
            var response = await _httpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }



        [Fact]
        public async Task RequestWithWrongSession_ShouldBeRejected()
        {
            // Arrange
            if (_httpClient == null || _client == null)
                throw new InvalidOperationException("Not initialized");

            var request = new HttpRequestMessage(HttpMethod.Get, "/plugins");
            request.Headers.Add("X-Session-Id", Guid.NewGuid().ToString());

            // Act
            var response = await _httpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task EachHandshake_ShouldGenerateUniqueSession()
        {
            // Arrange
            if (_httpClient == null)
                throw new InvalidOperationException("Not initialized");

            var client1 = new Client();
            var client2 = new Client();

            // Act
            await PerformHandshakeForClient(client1);
            await PerformHandshakeForClient(client2);

            // Assert
            client1.SessionId.Should().NotBeNullOrEmpty();
            client2.SessionId.Should().NotBeNullOrEmpty();
            client1.SessionId.Should().NotBe(client2.SessionId);
        }

        [Fact]
        public async Task EncryptedResponse_ShouldOnlyBeDecryptableByCorrectClient()
        {
            // Arrange
            if (_httpClient == null || _client == null)
                throw new InvalidOperationException("Not initialized");

            var client1 = new Client();
            var client2 = new Client();

            await PerformHandshakeForClient(client1);
            await PerformHandshakeForClient(client2);

            // Act
            var request = new HttpRequestMessage(HttpMethod.Get, "/plugins");
            request.Headers.Add("X-Session-Id", client1.SessionId);
            var response = await _httpClient.SendAsync(request);

            var encryptedResponse = await response.Content.ReadAsStringAsync();

            // Assert
            var decrypted1 = client1.DecryptMessage(encryptedResponse);
            decrypted1.Should().NotBeNullOrEmpty();

            Action decryptWithWrongClient = () => client2.DecryptMessage(encryptedResponse);
            decryptWithWrongClient.Should().Throw<System.Security.Cryptography.CryptographicException>();
        }

        private async Task PerformHandshakeForClient(Client client)
        {
            if (_httpClient == null)
                throw new InvalidOperationException("Not initialized");

            var publicKey = client.GetClientPublicKey();
            var request = new HandshakeRequest(publicKey);

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("/handshake/init", jsonContent);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var handshakeResponse = JsonSerializer.Deserialize<HandshakeResponse>(responseBody, options);

            if (handshakeResponse == null || string.IsNullOrEmpty(handshakeResponse.PublicKey))
                throw new InvalidOperationException("Invalid handshake response");

            var serverPublicKeyBytes = Convert.FromBase64String(handshakeResponse.PublicKey);
            client.CompleteHandshake(serverPublicKeyBytes, handshakeResponse.sessionId);
        }


    }
}
