using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Netch.Forms;
using Netch.Models;

namespace Netch.Services
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddViews(this IServiceCollection services)
        {
            services.TryAddSingleton<MainForm>();
            return services;
        }

        public static IServiceCollection AddSetting(this IServiceCollection services)
        {
            services.TryAddSingleton<Setting>();
            return services;
        }

        public static IServiceCollection AddDynamicData(this IServiceCollection services)
        {
            return services;
        }

        public static IServiceCollection AddStartupService(this IServiceCollection services)
        {
            return services;
        }
    }
}