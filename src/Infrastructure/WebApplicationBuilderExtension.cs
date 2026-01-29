
using Microsoft.Extensions.DependencyInjection;
using RemoteAgent.Domain.Interface;
using RemoteAgent.Infrastructure.Security;
using RemoteAgent.Infrastructure.Services;

namespace RemoteAgent.Infrastructure
{
    public static class WebApplicationBuilderExtension
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            services.AddSingleton<ISecretStore, SecretStore>();
            services.AddSingleton<IPluginService, PluginService>();
            services.AddSingleton<IEncryptionService, EncryptionService>();
            return services;
        }
    }
}
