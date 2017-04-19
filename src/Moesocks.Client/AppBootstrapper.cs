using System;
using System.Collections.Generic;
using Caliburn.Micro;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace Moesocks.Client
{
    public class AppBootstrapper : BootstrapperBase
    {
        public IServiceProvider ServiceProvider { get; private set; }
        private IConnectionRouter _connectionRouter;

        public AppBootstrapper()
        {
            Initialize();
            base.Application.DispatcherUnhandledException += Application_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            
        }

        protected override void Configure()
        {
            var startup = new Startup();

            var serviceCollection = new ServiceCollection();
            startup.ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();
            startup.DoConfigure(ServiceProvider);
        }

        protected override object GetInstance(Type service, string key)
        {
            return ServiceProvider.GetRequiredService(service);
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return ServiceProvider.GetServices(service);
        }

        protected override void BuildUp(object instance)
        {

        }

        protected override void OnExit(object sender, EventArgs e)
        {
            base.OnExit(sender, e);
            _connectionRouter?.Stop();
        }

        protected override void OnStartup(object sender, System.Windows.StartupEventArgs e)
        {
            StartConnectionRouter();
            DisplayRootViewFor<IShell>();
        }

        private void StartConnectionRouter()
        {
            _connectionRouter = IoC.Get<IConnectionRouter>();
            _connectionRouter.Startup();
        }
    }
}