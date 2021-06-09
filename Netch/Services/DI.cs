using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;
using System;

namespace Netch.Services
{
	public static class DI
	{
        public static T GetRequiredService<T>()
		{
			var service = Locator.Current.GetService<T>();

			if (service is null)
			{
				throw new InvalidOperationException($@"No service for type {typeof(T)} has been registered.");
			}

			return service;
		}

        public static void CreateLogger()
        {
            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
                .WriteTo.Async(c => c.Debug(outputTemplate: Constants.OutputTemplate))
#else
                .MinimumLevel.Information()
                .WriteTo.Async(c => c.File(Path.Combine(Global.NetchDir, Constants.LogFile),
                    outputTemplate: Constants.OutputTemplate,
                    rollOnFileSizeLimit: false))
#endif
                .WriteTo.Async(c => c.Console(outputTemplate: Constants.OutputTemplate))
                .MinimumLevel.Override(@"Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .CreateLogger();
        }

		public static void Register()
		{
			var services = new ServiceCollection();

			services.UseMicrosoftDependencyResolver();
			Locator.CurrentMutable.InitializeSplat();
			// Locator.CurrentMutable.InitializeReactiveUI(RegistrationNamespace.WinForms);

			ConfigureServices(services);
		}

		private static IServiceCollection ConfigureServices(IServiceCollection services)
		{
			services.AddViews()
					.AddSetting()
					.AddDynamicData()
					.AddStartupService()
					.AddLogging(c => c.AddSerilog());

			return services;
		}
	}
}
