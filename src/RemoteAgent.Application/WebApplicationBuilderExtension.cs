using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RemoteAgent.Application.Handshake.Commands;
using RemoteAgent.Application.Handshake.Handlers;
using RemoteAgent.Domain.Interface;
using RemoteAgent.Application.Plugin.Commands;
using RemoteAgent.Application.Plugin.Handlers;
using RemoteAgent.Application.Plugin.Queries;

namespace RemoteAgent.Application
{
    public static class WebApplicationBuilderExtension
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<ICommandHandler<HandshakeInitCommand>, HandshakeInitCommandHandler>();
            services.AddSingleton<ICommandHandler<UploadPluginCommand>, UploadPluginCommandHandler>();
            services.AddSingleton<ICommandHandler<UnloadPluginCommand>, UnloadPluginCommandHandler>();
            services.AddSingleton<ICommandHandler<ExecutePluginCommand>, ExecutePluginCommandHandler>();
            
            services.AddSingleton<IQueryHandler<GetPluginsQuery, IEnumerable<PluginInfo>>, GetPluginsQueryHandler>();
            
            return services;
        }
    }
}
