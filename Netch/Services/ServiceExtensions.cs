using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Netch.Forms;
using Netch.Models;
using Netch.ViewModels;

namespace Netch.Services
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddViews(this IServiceCollection services)
        {
            services.TryAddSingleton<MainForm>();
            services.TryAddTransient<SettingForm>();
            services.TryAddTransient<SubscribeForm>();
            services.TryAddTransient<GlobalBypassIPForm>();

            return services;
        }

        public static IServiceCollection AddViewModels(this IServiceCollection services)
        {
            services.TryAddSingleton<MainWindowViewModel>();

            return services;
        }

        public static IServiceCollection AddSetting(this IServiceCollection services)
        {
            services.TryAddSingleton<Configuration>();
            services.TryAddSingleton<Setting>();

            return services;
        }

        public static IServiceCollection AddDynamicData(this IServiceCollection services)
        {
            services.TryAddSingleton<SourceList<Server>>();
            services.TryAddSingleton(new SourceCache<Mode, string>(mode => mode.FullName ?? mode.Remark));

            return services;
        }

        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.TryAddSingleton<ModeService>();

            return services;
        }

        public static IServiceCollection AddStartupService(this IServiceCollection services)
        {
            return services;
        }
    }
}