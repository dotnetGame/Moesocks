using System;
using System.Collections.Generic;
using Caliburn.Micro;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Moesocks.Client.Views;
using Moesocks.Client.ViewModels;
using System.Windows;
using System.Threading;
using Moesocks.Client.Services;
using Hardcodet.Wpf.TaskbarNotification;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Moesocks.Client
{
    public class AppBootstrapper : BootstrapperBase
    {
        public IServiceProvider ServiceProvider { get; private set; }
        private IConnectionRouter _connectionRouter;
        private readonly Mutex _singletonMutex;

        public AppBootstrapper()
        {
            bool createdNew;
            _singletonMutex = new Mutex(true, "Moesocks Client", out createdNew);
            if(!createdNew)
            {
                MessageBox.Show("Moesocks 已经启动。", "Moesocks", MessageBoxButton.OK, MessageBoxImage.Information);
                Environment.Exit(0);
            }

            Initialize();
            base.Application.DispatcherUnhandledException += Application_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            ReportError(e.Exception);
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
            ReportError(e.Exception.Flatten());
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                ReportError(ex);
        }

        private ILogger _logger;
        private bool _isMessageDisplaying;

        private void ReportError(Exception exception)
        {
            if(_logger == null)
            {
                var loggerFactory = ServiceProvider?.GetService<ILoggerFactory>();
                if (loggerFactory != null)
                    _logger = loggerFactory.CreateLogger<AppBootstrapper>();
            }
            if (_logger != null)
                _logger.LogError(default(EventId), exception.Message, exception);
            else if(!_isMessageDisplaying)
            {
                _isMessageDisplaying = true;
                MessageBox.Show(exception.Message.ToString(), "Moesocks", MessageBoxButton.OK, MessageBoxImage.Error);
                _isMessageDisplaying = false;
            }
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

        private SystemTrayIconView _trayIcon;

        protected override void OnStartup(object sender, System.Windows.StartupEventArgs e)
        {
            EnableTrayIcon();
            StartConnectionRouter();
            StartUpdateService();
        }

        private IUpdateService _updateService;
        private void StartUpdateService()
        {
            _updateService = IoC.Get<IUpdateService>();
            _updateService.NewReleaseFound += updateService_NewReleaseFound;
            _updateService.Startup();
        }

        private void updateService_NewReleaseFound(object sender, NewReleaseFoundEventArgs e)
        {
            RoutedEventHandler balloonTipClicked = null;
            balloonTipClicked = (s, _) =>
            {
                _trayIcon.TrayBalloonTipClicked -= balloonTipClicked;
                Process.Start("explorer.exe", string.Format("/select,\"{0}\"", e.UpdatePack.FullName));
            };
            _trayIcon.TrayBalloonTipClicked += balloonTipClicked;
            _trayIcon.ShowBalloonTip("发现新版本", $"发现新版本 {e.Version}， 点击这里立即更新。", BalloonIcon.Info);
        }

        private void EnableTrayIcon()
        {
            _trayIcon = new SystemTrayIconView();
            ViewModelBinder.Bind(IoC.Get<SystemTrayIconViewModel>(), _trayIcon, null);
        }

        private void StartConnectionRouter()
        {
            _connectionRouter = IoC.Get<IConnectionRouter>();
            _connectionRouter.Startup();
        }
    }
}