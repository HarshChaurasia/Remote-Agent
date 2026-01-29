using Moq;
using RemoteAgent.Application.Plugin.Commands;
using RemoteAgent.Application.Plugin.Handlers;
using RemoteAgent.Domain.Interface;

namespace RemoteAgent.UnitTests.Handlers
{
    public class UploadPluginCommandHandlerTests
    {
        private readonly Mock<IPluginService> _pluginServiceMock;
        private readonly UploadPluginCommandHandler _handler;

        public UploadPluginCommandHandlerTests()
        {
            _pluginServiceMock = new Mock<IPluginService>();
            _handler = new UploadPluginCommandHandler(_pluginServiceMock.Object);
        }

        [Fact]
        public async Task Handle_WithValidCommand_ReturnsSuccess()
        {
            // Arrange
            var dllBase64 = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 });
            var pluginName = "TestPlugin";
            var command = new UploadPluginCommand(dllBase64, pluginName);
            var expectedResult = "Plugin loaded successfully";

            _pluginServiceMock
                .Setup(x => x.LoadPluginAsync(It.IsAny<byte[]>(), pluginName))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedResult, result.Response);
            _pluginServiceMock.Verify(x => x.LoadPluginAsync(It.IsAny<byte[]>(), pluginName), Times.Once);
        }

        [Fact]
        public async Task Handle_WithInvalidBase64_ReturnsFailure()
        {
            // Arrange
            var invalidBase64 = "!!!inv---alid!!!";
            var pluginName = "TestPlugin";
            var command = new UploadPluginCommand(invalidBase64, pluginName);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("encoding is invalid", result.Response?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task Handle_WhenPluginLoadFails_ReturnsFailure()
        {
            // Arrange
            var dllBase64 = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 });
            var pluginName = "TestPlugin";
            var command = new UploadPluginCommand(dllBase64, pluginName);

            _pluginServiceMock
                .Setup(x => x.LoadPluginAsync(It.IsAny<byte[]>(), pluginName))
                .ThrowsAsync(new InvalidOperationException("No IPlugin found"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("No IPlugin found", result.Response?.ToString() ?? string.Empty);
        }
    }
}
