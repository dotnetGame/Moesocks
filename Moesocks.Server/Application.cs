using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Moesocks.Server
{
    class Application
    {
        private readonly IServiceProvider _serviceProvider;

        public Application(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Startup()
        {
            var clientTier = ActivatorUtilities.CreateInstance<ClientTier>(_serviceProvider);
            clientTier.Start();
            while (Console.ReadKey().Key != ConsoleKey.Escape) ;
        }
    }
}
