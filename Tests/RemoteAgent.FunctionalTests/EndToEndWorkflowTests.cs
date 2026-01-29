using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using RemoteAgent.WebAPI;
using RemoteAgent.FunctionalTests.Fixtures;
using RemoteAgent.WebAPI.Model;

namespace RemoteAgent.FunctionalTests
{
    /// <summary>
    /// Functional tests for validating end to end flow of all the usecases.
    /// </summary>
    public class EndToEndWorkflowTests : IAsyncLifetime
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
        public async Task CompleteWorkflow_UploadLoadExecuteUnload_ShouldSucceed()
        {
            // Arrange
            if (_httpClient == null || _client == null)
                throw new InvalidOperationException("Not initialized");

            var dllBytes = LoadPluginDll("WindowsPlugin");

            // Step 1: Upload plugin using encrypted JSON (file as base64 string)
            var uploadPayload = new
            {
                dllFile = Convert.ToBase64String(dllBytes),
                name = "WindowsPlugin"
            };

            var encryptedUpload = _client.EncryptMessage(JsonSerializer.Serialize(uploadPayload));
            var uploadRequest = new HttpRequestMessage(HttpMethod.Post, "/plugins")
            {
                Content = new StringContent(encryptedUpload, Encoding.UTF8, "application/json")
            };
            uploadRequest.Headers.Add("X-Session-Id", _client.SessionId);

            var uploadResponse = await _httpClient.SendAsync(uploadRequest);
            uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Step 2: List plugins (encrypted)
            var listRequest = new HttpRequestMessage(HttpMethod.Get, "/plugins");
            listRequest.Headers.Add("X-Session-Id", _client.SessionId);
            var listResponse = await _httpClient.SendAsync(listRequest);
            listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var listBody = await listResponse.Content.ReadAsStringAsync();
            var decryptedList = _client.DecryptMessage(listBody);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var plugins = JsonSerializer.Deserialize<List<PluginInfo>>(decryptedList, options);
            plugins.Should().NotBeNull();
            plugins.Should().Contain(p => p.Name == "WindowsPlugin");

            // Step 3: Execute plugin (encrypted)
            var executeRequest = new ExecuteRequest
            {
                TargetOS = "Windows",
                Version = "10.0",
                Command = "info"
            };

            var encryptedExecute = _client.EncryptMessage(JsonSerializer.Serialize(executeRequest));
            var executeHttpRequest = new HttpRequestMessage(HttpMethod.Post, "/execute")
            {
                Content = new StringContent(encryptedExecute, Encoding.UTF8, "application/json")
            };
            executeHttpRequest.Headers.Add("X-Session-Id", _client.SessionId);

            var executeResponse = await _httpClient.SendAsync(executeHttpRequest);
            executeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var executeBody = await executeResponse.Content.ReadAsStringAsync();
            var decryptedExecute = _client.DecryptMessage(executeBody);
            var executionResult = JsonSerializer.Deserialize<ExecuteResponse>(decryptedExecute, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            executionResult.Should().NotBeNull();
            executionResult!.Success.Should().BeTrue();

            // Step 4: Unload plugin (encrypted)
            var unloadRequest = new HttpRequestMessage(HttpMethod.Delete, "/plugins/WindowsPlugin");
            unloadRequest.Headers.Add("X-Session-Id", _client.SessionId);
            var unloadResponse = await _httpClient.SendAsync(unloadRequest);
            unloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Step 5: Verify plugin is unloaded (encrypted)
            var verifyRequest = new HttpRequestMessage(HttpMethod.Get, "/plugins");
            verifyRequest.Headers.Add("X-Session-Id", _client.SessionId);
            var verifyResponse = await _httpClient.SendAsync(verifyRequest);
            var verifyBody = await verifyResponse.Content.ReadAsStringAsync();
            var decryptedVerify = _client.DecryptMessage(verifyBody);
            var verifyOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var verifyPlugins = JsonSerializer.Deserialize<List<PluginInfo>>(decryptedVerify, verifyOptions);
            verifyPlugins.Should().NotContain(p => p.Name == "WindowsPlugin");
        }



        [Fact]
        public async Task MultipleClients_IndependentSessions_ShouldWork()
        {
            // Arrange
            if (_httpClient == null)
                throw new InvalidOperationException("Not initialized");

            var client1 = new Client();
            var client2 = new Client();

            await PerformHandshakeForClient(client1);
            await PerformHandshakeForClient(client2);

            // Act 
            var dllBytes = LoadPluginDll("WindowsPlugin");
            var uploadPayload = new
            {
                dllFile = Convert.ToBase64String(dllBytes),
                name = "WindowsPlugin"
            };

            var encryptedUpload = client1.EncryptMessage(JsonSerializer.Serialize(uploadPayload));
            var uploadRequest = new HttpRequestMessage(HttpMethod.Post, "/plugins")
            {
                Content = new StringContent(encryptedUpload, Encoding.UTF8, "application/json")
            };
            uploadRequest.Headers.Add("X-Session-Id", client1.SessionId);
            var response1 = await _httpClient.SendAsync(uploadRequest);
            response1.StatusCode.Should().Be(HttpStatusCode.OK);

            var listRequest = new HttpRequestMessage(HttpMethod.Get, "/plugins");
            listRequest.Headers.Add("X-Session-Id", client2.SessionId);
            var listResponse = await _httpClient.SendAsync(listRequest);
            listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var listBody = await listResponse.Content.ReadAsStringAsync();
            var decrypted = client2.DecryptMessage(listBody);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var plugins = JsonSerializer.Deserialize<List<PluginInfo>>(decrypted, options);

            // Assert
            plugins.Should().Contain(p => p.Name == "WindowsPlugin");
        }



        [Fact]
        public async Task ConcurrentPluginOperations_ShouldHandleGracefully()
        {
            // Arrange
            if (_httpClient == null || _client == null)
                throw new InvalidOperationException("Not initialized");

            var dllBytes = LoadPluginDll("WindowsPlugin");

            // Act
            var tasks = Enumerable.Range(0, 5).Select(async i =>
            {
                var uploadPayload = new
                {
                    dllFile = Convert.ToBase64String(dllBytes),
                    name = $"ConcurrentPlugin{i}"
                };

                var encryptedUpload = _client.EncryptMessage(JsonSerializer.Serialize(uploadPayload));
                var request = new HttpRequestMessage(HttpMethod.Post, "/plugins")
                {
                    Content = new StringContent(encryptedUpload, Encoding.UTF8, "application/json")
                };
                request.Headers.Add("X-Session-Id", _client.SessionId);
                return await _httpClient.SendAsync(request);
            });

            var responses = await Task.WhenAll(tasks);

            // Assert
            responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));
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

        private byte[] LoadPluginDll(string pluginName)
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var pluginDllPath = Path.Combine(basePath, $"{pluginName}.dll");

            if (File.Exists(pluginDllPath))
            {
                return File.ReadAllBytes(pluginDllPath);
            }

            throw new FileNotFoundException($"Could not locate {pluginName}.dll");
        }
    }

    public class PluginInfo
    {
        public string Name { get; set; } = string.Empty;
        public string TargetOS { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public DateTime LoadedAt { get; set; }
    }
}
