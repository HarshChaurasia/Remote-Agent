using RemoteAgent.Domain.Interface;
using RemoteAgent.PluginsContract;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;

namespace RemoteAgent.Infrastructure.Services
{

    /// <summary>
    /// PluginService used to load, unload, get and execute plugins
    /// </summary>
    public class PluginService : IPluginService
    {
        private readonly ConcurrentDictionary<string, PluginContext> _plugins = new();
        private readonly object _lock = new();

        public async Task<string> LoadPluginAsync(byte[] dllBytes, string pluginName)
        {
            if (_plugins.ContainsKey(pluginName))
            {
                await UnloadPlugin(pluginName);
            }

            lock (_lock)
            {
                var context = new AssemblyLoadContext(pluginName, isCollectible: true);

                Assembly assembly;
                using (var stream = new MemoryStream(dllBytes))
                {
                    assembly = context.LoadFromStream(stream);
                }

                // using reflection to find all types implementing IPlugin
                var pluginTypes = assembly.GetTypes()
                    .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    .ToList();

                if (pluginTypes.Count == 0)
                    throw new InvalidOperationException($"Assembly {pluginName} is invalid, as IPlugin is not implemented.");

                var pluginInstance = Activator.CreateInstance(pluginTypes[0]) as IPlugin;
                if (pluginInstance == null)
                    throw new InvalidOperationException($"Incorrect implementation of Iplugin found in {pluginName}.");

                _plugins[pluginName] = new PluginContext
                {
                    Name = pluginName,
                    Plugin = pluginInstance,
                    Context = context,
                    Assembly = assembly,
                    LoadedAt = DateTime.UtcNow
                };

                return $"'{pluginName}' loaded successfully. TargetOS: {pluginInstance.TargetOS}, Version: {pluginInstance.Version}";
            }
        }

        public IEnumerable<PluginInfo> GetLoadedPlugins()
        {
            lock (_lock)
            {
                return _plugins.Values.Select(p => new PluginInfo
                {
                    Name = p.Name,
                    TargetOS = p.Plugin.TargetOS,
                    Version = p.Plugin.Version,
                    LoadedAt = p.LoadedAt
                }).ToList();
            }
        }

        public async Task UnloadPlugin(string pluginName)
        {
            if (string.IsNullOrEmpty(pluginName))
                return;

            lock (_lock)
            {
                if (!_plugins.TryGetValue(pluginName, out var pluginContext))
                    return;

                // Unloading the context 
                pluginContext.Context.Unload();
                _plugins.Remove(pluginName, out _);

                // Forcing garbage collection 
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        public async Task<string> ExecutePluginAsync(string targetOS, string version, string command)
        {

            PluginContext? pluginContext = null;

            lock (_lock)
            {
                pluginContext = _plugins.Values
                    .FirstOrDefault(p => string.Equals(p.Plugin.TargetOS, targetOS, StringComparison.OrdinalIgnoreCase) &&
                                         string.Equals(p.Plugin.Version, version, StringComparison.OrdinalIgnoreCase));
            }

            if (pluginContext == null)
                throw new InvalidOperationException($"No plugin found for TargetOS: {targetOS}, Version: {version}");

            try
            {
                return await pluginContext.Plugin.ExecuteAsync(command);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error executing plugin '{pluginContext.Name}': {ex.Message}", ex);
            }
        }

        private class PluginContext
        {
            public string Name { get; set; } = string.Empty;
            public IPlugin Plugin { get; set; } = null!;
            public AssemblyLoadContext Context { get; set; } = null!;
            public Assembly Assembly { get; set; } = null!;
            public DateTime LoadedAt { get; set; }
        }

    }
}
