using Moq;
using RemoteAgent.Application.Plugin.Commands;
using RemoteAgent.Application.Plugin.Handlers;
using RemoteAgent.Domain.Common;
using RemoteAgent.Domain.Interface;

namespace RemoteAgent.UnitTests.Handlers
{
    public class UnloadPluginCommandHandlerTests
    {
        private readonly Mock<IPluginService> _pluginServiceMock;
        private readonly UnloadPluginCommandHandler _handler;

        public UnloadPluginCommandHandlerTests()
        {
            _pluginServiceMock = new Mock<IPluginService>();
            _handler = new UnloadPluginCommandHandler(_pluginServiceMock.Object);
        }

        [Fact]
        public async Task Handle_WithValidPluginName_ReturnsSuccess()
        {
            // Arrange
            var pluginName = "TestPlugin";
            var command = new UnloadPluginCommand(pluginName);

            _pluginServiceMock
                .Setup(x => x.UnloadPlugin(pluginName))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains(pluginName, result.Response?.ToString() ?? string.Empty);
            Assert.Contains("unloaded successfully", result.Response?.ToString() ?? string.Empty);
            _pluginServiceMock.Verify(x => x.UnloadPlugin(pluginName), Times.Once);
        }

        [Fact]
        public async Task Handle_WithDifferentPluginNames_UnloadsCorrectPlugin()
        {
            // Arrange
            var pluginName1 = "Plugin1";
            var pluginName2 = "Plugin2";
            var command = new UnloadPluginCommand(pluginName1);

            _pluginServiceMock
                .Setup(x => x.UnloadPlugin(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            _pluginServiceMock.Verify(x => x.UnloadPlugin(pluginName1), Times.Once);
            _pluginServiceMock.Verify(x => x.UnloadPlugin(pluginName2), Times.Never);
        }

    }
}
