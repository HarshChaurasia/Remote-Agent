using RemoteAgent.Domain.Interface;
using System.Text;

namespace RemoteAgent.WebAPI.Middleware
{
    /// <summary>
    /// Middle for encryption/decryption
    /// </summary>
    public class EncryptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IEncryptionService _encryptionService;
        private readonly ISecretStore _secretStore;
        private const string SessionIdHeader = "X-Session-Id";

        public EncryptionMiddleware(
            RequestDelegate next,
            IEncryptionService encryptionService,
            ISecretStore secretStore)
        {
            _next = next;
            _encryptionService = encryptionService;
            _secretStore = secretStore;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skipping encryption for handshake endpoint 
            if (context.Request.Path.StartsWithSegments("/handshake") || context.Request.Path.StartsWithSegments("/favicon.ico"))
            {
                await _next(context);
                return;
            }

            // Get session ID from header
            if (!context.Request.Headers.TryGetValue(SessionIdHeader, out var sessionIdHeader) ||
                string.IsNullOrEmpty(sessionIdHeader))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Missing session ID. Please perform handshake first.");
                return;
            }

            var sessionId = sessionIdHeader.ToString();
            var sharedSecret = await _secretStore.TryGetKeyAsync(sessionId, CancellationToken.None);

            if (sharedSecret == null)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid or expired session. Please perform handshake again.");
                return;
            }

            // Decrypt request body if present
            if (context.Request.ContentLength > 0 &&
                context.Request.ContentType?.Contains("application/json") == true)
            {
                context.Request.EnableBuffering();
                var originalBodyStream = context.Request.Body;

                try
                {
                    // Read encrypted body
                    context.Request.Body.Position = 0;
                    using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                    var encryptedBody = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0;

                    if (!string.IsNullOrWhiteSpace(encryptedBody))
                    {
                        try
                        {
                            var decryptedBody = _encryptionService.Decrypt(encryptedBody, sharedSecret);
                            var decryptedBytes = Encoding.UTF8.GetBytes(decryptedBody);
                            context.Request.Body = new MemoryStream(decryptedBytes);
                            context.Request.ContentLength = decryptedBytes.Length;
                        }
                        catch (Exception ex)
                        {
                            context.Response.StatusCode = 403;
                            await context.Response.WriteAsync($"Decryption failed: {ex.Message}");
                            return;
                        }
                    }
                }
                finally
                {
                    // Only dispose if we haven't replaced the stream
                    if (context.Request.Body == originalBodyStream)
                    {
                        originalBodyStream.Dispose();
                    }
                }
            }

            // Capture response body
            var originalResponseBody = context.Response.Body;
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            try
            {
                await _next(context);
            }
            finally
            {
                // Encrypt response body
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();

                if (!string.IsNullOrEmpty(responseBody) && context.Response.StatusCode == 200)
                {
                    try
                    {
                        var encryptedResponse = _encryptionService.Encrypt(responseBody, sharedSecret);
                        var encryptedBytes = Encoding.UTF8.GetBytes(encryptedResponse);

                        context.Response.Body = originalResponseBody;
                        context.Response.ContentLength = encryptedBytes.Length;
                        await context.Response.WriteAsync(encryptedResponse);
                    }
                    catch (Exception ex)
                    {
                        context.Response.StatusCode = 500;
                        context.Response.Body = originalResponseBody;
                        await context.Response.WriteAsync($"Encryption failed: {ex.Message}");
                    }
                }
                else
                {
                    context.Response.Body = originalResponseBody;
                    if (responseBodyStream.Length > 0)
                    {
                        responseBodyStream.Seek(0, SeekOrigin.Begin);
                        await responseBodyStream.CopyToAsync(originalResponseBody);
                    }
                }
            }
        }
    }
}
