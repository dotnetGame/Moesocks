using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Moesocks.Server
{
    class Program
    {
        public static IConfiguration Configuration { get; private set; }
        public static IServiceProvider ServiceProvider { get; private set; }

        static void Main(string[] args)
        {
            Configuration = LoadConfiguration();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();
            DoConfigure(ServiceProvider);
            
            //AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            Startup();
        }

        private static void DoConfigure(IServiceProvider serviceProvider)
        {
            var method = typeof(Program).GetMethod(nameof(Configure), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var @params = from p in method.GetParameters()
                          select serviceProvider.GetRequiredService(p.ParameterType);
            method.Invoke(null, @params.ToArray());
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
            Console.WriteLine(e.Exception.Flatten().ToString());
        }

        //private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        //{
        //    Console.WriteLine(e.ExceptionObject.ToString());
        //}

        private static void Startup()
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                ActivatorUtilities.CreateInstance<App>(scope.ServiceProvider).Startup();
            }
        }

        private static IConfiguration LoadConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json", false, false);
            return builder.Build();
        }

        private static void ConfigureServices(IServiceCollection serviceProvider)
        {
            serviceProvider.AddOptions();
            serviceProvider.AddLogging();
            serviceProvider.AddConnectionRouter(Configuration.GetSection("connectionRouter"));
        }

        private static void Configure(ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();
        }
    }
}