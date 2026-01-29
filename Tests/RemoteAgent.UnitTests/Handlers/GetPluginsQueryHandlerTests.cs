using Moq;
using RemoteAgent.Application.Plugin.Handlers;
using RemoteAgent.Application.Plugin.Queries;
using RemoteAgent.Domain.Interface;

namespace RemoteAgent.UnitTests.Handlers
{
    public class GetPluginsQueryHandlerTests
    {
        private readonly Mock<IPluginService> _pluginServiceMock;
        private readonly GetPluginsQueryHandler _handler;

        public GetPluginsQueryHandlerTests()
        {
            _pluginServiceMock = new Mock<IPluginService>();
            _handler = new GetPluginsQueryHandler(_pluginServiceMock.Object);
        }

        [Fact]
        public async Task Handle_WithNoPluginsLoaded_ReturnsEmptyList()
        {
            // Arrange
            var query = new GetPluginsQuery();
            var emptyPlugins = new List<PluginInfo>();

            _pluginServiceMock
                .Setup(x => x.GetLoadedPlugins())
                .Returns(emptyPlugins);

            // Act
            var result = await _handler.Handle(query);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _pluginServiceMock.Verify(x => x.GetLoadedPlugins(), Times.Once);
        }

        [Fact]
        public async Task Handle_WithSinglePluginLoaded_ReturnsPluginList()
        {
            // Arrange
            var query = new GetPluginsQuery();
            var plugins = new List<PluginInfo>
            {
                new PluginInfo
                {
                    Name = "WindowsPlugin",
                    TargetOS = "Windows",
                    Version = "10.0",
                    LoadedAt = DateTime.UtcNow
                }
            };

            _pluginServiceMock
                .Setup(x => x.GetLoadedPlugins())
                .Returns(plugins);

            // Act
            var result = await _handler.Handle(query);

            // Assert
            Assert.NotNull(result);
            var pluginList = result.ToList();
            Assert.Single(pluginList);
            Assert.Equal("WindowsPlugin", pluginList[0].Name);
            Assert.Equal("Windows", pluginList[0].TargetOS);
            Assert.Equal("10.0", pluginList[0].Version);
        }

        [Fact]
        public async Task Handle_WithMultiplePluginsLoaded_ReturnsAllPlugins()
        {
            // Arrange
            var query = new GetPluginsQuery();
            var plugins = new List<PluginInfo>
            {
                new PluginInfo
                {
                    Name = "WindowsPlugin",
                    TargetOS = "Windows",
                    Version = "10.0",
                    LoadedAt = DateTime.UtcNow
                },
                new PluginInfo
                {
                    Name = "LinuxPlugin",
                    TargetOS = "Linux",
                    Version = "10.0",
                    LoadedAt = DateTime.UtcNow
                },
                new PluginInfo
                {
                    Name = "MacPlugin",
                    TargetOS = "Mac",
                    Version = "10.0",
                    LoadedAt = DateTime.UtcNow
                }
            };

            _pluginServiceMock
                .Setup(x => x.GetLoadedPlugins())
                .Returns(plugins);

            // Act
            var result = await _handler.Handle(query);

            // Assert
            Assert.NotNull(result);
            var pluginList = result.ToList();
            Assert.Equal(3, pluginList.Count);
            Assert.Contains(pluginList, p => p.Name == "WindowsPlugin");
            Assert.Contains(pluginList, p => p.Name == "LinuxPlugin");
            Assert.Contains(pluginList, p => p.Name == "MacPlugin");
        }

    }
}
