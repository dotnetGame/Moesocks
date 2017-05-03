using Caliburn.Micro;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moesocks.Client.Logging;
using Moesocks.Client.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moesocks.Client
{
    class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json")
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                   .AddEnvironmentVariables();
            Configuration = builder.Build();
        }
        
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();
            services.AddOptions();
            services.AddAppConfiguration("config.json");
            services.AddSingleton<IProductInformation, ProductInformation>();
            services.AddSingleton<IWindowManager>(new WindowManager());
            services.AddSingleton<IEventAggregator>(new EventAggregator());

            services.AddTransient<IShell, ShellViewModel>();
            services.AddTransient<SystemTrayIconViewModel>();
            services.AddTransient<Areas.Pages.ViewModels.LoggingViewModel>();
            services.AddTransient<Areas.Pages.ViewModels.SettingsViewModel>();
            services.AddSingleton<FlowDocumentLoggerProvider>();

            services.AddConnectionRouter(Configuration.GetSection("connectionRouter"));
            services.AddSecurity(Configuration.GetSection("security"));
            services.AddUpdate();
        }

        public void DoConfigure(IServiceProvider serviceProvider)
        {
            var method = typeof(Startup).GetMethod(nameof(Configure), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var @params = from p in method.GetParameters()
                          select serviceProvider.GetRequiredService(p.ParameterType);
            method.Invoke(this, @params.ToArray());
        }

        private void Configure(ILoggerFactory loggerFactory, FlowDocumentLoggerProvider flowDocLogger)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddProvider(flowDocLogger);
            loggerFactory.AddDebug();
        }
    }
}
