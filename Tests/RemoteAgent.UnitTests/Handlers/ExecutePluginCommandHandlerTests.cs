using Moq;
using RemoteAgent.Application.Plugin.Commands;
using RemoteAgent.Application.Plugin.Handlers;
using RemoteAgent.Domain.Interface;

namespace RemoteAgent.UnitTests.Handlers
{
    public class ExecutePluginCommandHandlerTests
    {
        private readonly Mock<IPluginService> _pluginServiceMock;
        private readonly ExecutePluginCommandHandler _handler;

        public ExecutePluginCommandHandlerTests()
        {
            _pluginServiceMock = new Mock<IPluginService>();
            _handler = new ExecutePluginCommandHandler(_pluginServiceMock.Object);
        }

        [Fact]
        public async Task Handle_WithValidCommand_ReturnsSuccess()
        {
            // Arrange
            var command = new ExecutePluginCommand("Windows", "10.0", "info");
            var expectedResult = "Execution result";

            _pluginServiceMock
                .Setup(x => x.ExecutePluginAsync("Windows", "10.0", "info"))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedResult, result.Response);
        }

        [Fact]
        public async Task Handle_WhenPluginNotFound_ReturnsFailure()
        {
            // Arrange
            var command = new ExecutePluginCommand("Windows", "10.0", "info");

            _pluginServiceMock
                .Setup(x => x.ExecutePluginAsync("Windows", "10.0", "info"))
                .ThrowsAsync(new InvalidOperationException("No plugin found"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("No plugin found", result.Response?.ToString() ?? string.Empty);
        }
    }
}
