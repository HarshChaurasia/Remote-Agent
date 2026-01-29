using RemoteAgent.Domain.Interface;
using RemoteAgent.Application.Plugin.Queries;

namespace RemoteAgent.Application.Plugin.Handlers
{
    public class GetPluginsQueryHandler : IQueryHandler<GetPluginsQuery, IEnumerable<PluginInfo>>
    {
        private readonly IPluginService _pluginService;

        public GetPluginsQueryHandler(IPluginService pluginService)
        {
            _pluginService = pluginService;
        }

        public Task<IEnumerable<PluginInfo>> Handle(GetPluginsQuery query)
        {
            var plugins = _pluginService.GetLoadedPlugins();
            return Task.FromResult(plugins);
        }
    }
}
