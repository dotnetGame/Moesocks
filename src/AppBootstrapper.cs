using System;
using System.Collections.Generic;
using Caliburn.Micro;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Moesocks.Client
{
    public class AppBootstrapper : BootstrapperBase
    {
        private IWebHost _webHost;

        public AppBootstrapper()
        {
            Initialize();
        }

        protected override void Configure()
        {
            _webHost = new WebHostBuilder()
                .UseWebProxy()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .Build();
        }

        protected override object GetInstance(Type service, string key)
        {
            return _webHost.Services.GetRequiredService(service);
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return _webHost.Services.GetServices(service);
        }

        protected override void BuildUp(object instance)
        {

        }

        protected override void OnStartup(object sender, System.Windows.StartupEventArgs e)
        {
            StartWebHost();
            DisplayRootViewFor<IShell>();
        }

        private void StartWebHost()
        {
            _webHost.Start();
            var env = _webHost.Services.GetService<IHostingEnvironment>();
            var lifeTime = _webHost.Services.GetService<IApplicationLifetime>();
            Console.WriteLine(string.Format("Hosting environment: {0}", env.EnvironmentName));
            Console.WriteLine(string.Format("Content root path: {0}", env.ContentRootPath));
            var addressFeature = _webHost.ServerFeatures.Get<IServerAddressesFeature>();
            var addresses = addressFeature?.Addresses;
            if (addresses != null)
            {
                foreach (string current in addresses)
                    Console.WriteLine(string.Format("Now listening on: {0}", current));
            }
        }
    }
}