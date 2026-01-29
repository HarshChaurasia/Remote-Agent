using RemoteAgent.Domain.Interface;
using RemoteAgent.Infrastructure.Services;
using Xunit;

namespace RemoteAgent.Infrastructure.UnitTests.Services
{
    public class PluginServiceTests
    {
        private readonly IPluginService _pluginService;

        public PluginServiceTests()
        {
            _pluginService = new PluginService();
        }

        [Fact]
        public async Task LoadPluginAsync_WithValidDllBytes_LoadsPluginSuccessfully()
        {
            // Arrange
            var dllPath = FindPluginDll("WindowsPlugin");
            var dllBytes = File.ReadAllBytes(dllPath);

            // Act
            var result = await _pluginService.LoadPluginAsync(dllBytes, "TestWindowsPlugin");

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains("loaded successfully", result);
        }

        [Fact]
        public async Task LoadPluginAsync_WithInvalidDllBytes_ThrowsException()
        {
            // Arrange
            var invalidDllBytes = new byte[] { 1, 2, 3, 4, 5 };

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(() =>
                _pluginService.LoadPluginAsync(invalidDllBytes, "InvalidPlugin"));
        }

        [Fact]
        public async Task GetLoadedPlugins_AfterLoading_ReturnsLoadedPlugins()
        {
            // Arrange
            var dllPath = FindPluginDll("WindowsPlugin");
            var dllBytes = File.ReadAllBytes(dllPath);
            await _pluginService.LoadPluginAsync(dllBytes, "TestWindowsPlugin");

            // Act
            var plugins = _pluginService.GetLoadedPlugins().ToList();

            // Assert
            Assert.NotEmpty(plugins);
            Assert.Contains(plugins, p => p.Name == "TestWindowsPlugin");
        }

        [Fact]
        public async Task UnloadPlugin_WithValidPluginName_UnloadsPlugin()
        {
            // Arrange
            var dllPath = FindPluginDll("WindowsPlugin");
            var dllBytes = File.ReadAllBytes(dllPath);
            await _pluginService.LoadPluginAsync(dllBytes, "TestWindowsPlugin");

            // Act
            await _pluginService.UnloadPlugin("TestWindowsPlugin");

            // Assert
            var plugins = _pluginService.GetLoadedPlugins().ToList();
            Assert.DoesNotContain(plugins, p => p.Name == "TestWindowsPlugin");
        }

        [Fact]
        public async Task UnloadPlugin_WithInvalidPluginName_DoesNotThrow()
        {
            // Act & Assert
            await _pluginService.UnloadPlugin("NonExistentPlugin");
        }

        [Fact]
        public async Task ExecutePluginAsync_WithValidParameters_ExecutesPlugin()
        {
            // Arrange
            var dllPath = FindPluginDll("WindowsPlugin");
            var dllBytes = File.ReadAllBytes(dllPath);
            await _pluginService.LoadPluginAsync(dllBytes, "TestWindowsPlugin");

            // Act
            var result = await _pluginService.ExecutePluginAsync("Windows", "1.0", "info");

            // Assert
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task ExecutePluginAsync_WithInvalidTargetOS_ThrowsInvalidOperationException()
        {
            // Arrange
            var dllPath = FindPluginDll("WindowsPlugin");
            var dllBytes = File.ReadAllBytes(dllPath);
            await _pluginService.LoadPluginAsync(dllBytes, "TestWindowsPlugin");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _pluginService.ExecutePluginAsync("Linux", "1.0", "info"));
        }

        [Fact]
        public async Task LoadMultiplePlugins_AllAreAvailable()
        {
            // Arrange
            var windowsDllPath = FindPluginDll("WindowsPlugin");
            var linuxDllPath = FindPluginDll("LinuxPlugin");
            var windowsDllBytes = File.ReadAllBytes(windowsDllPath);
            var linuxDllBytes = File.ReadAllBytes(linuxDllPath);

            // Act
            await _pluginService.LoadPluginAsync(windowsDllBytes, "TestWindowsPlugin");
            await _pluginService.LoadPluginAsync(linuxDllBytes, "TestLinuxPlugin");

            var plugins = _pluginService.GetLoadedPlugins().ToList();

            // Assert
            Assert.Equal(2, plugins.Count);
            Assert.Contains(plugins, p => p.Name == "TestWindowsPlugin");
            Assert.Contains(plugins, p => p.Name == "TestLinuxPlugin");
        }

        private string FindPluginDll(string pluginName)
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var dllPaths = Path.Combine(basePath, $"{pluginName}.dll");

            if (File.Exists(dllPaths))
                return dllPaths;

            throw new FileNotFoundException($"Could not locate {pluginName}.dll");
        }
    }
}
