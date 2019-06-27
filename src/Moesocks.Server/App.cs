using Moesocks.Server.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Moesocks.Server
{
    class App
    {
        public IServiceProvider ServiceProvider { get; }
        private readonly IConnectionRouter _connectionRouter;
        private readonly ManualResetEvent _quitEvent = new ManualResetEvent(false);

        public App(IServiceProvider serviceProvider, IConnectionRouter connectionRouter)
        {
            ServiceProvider = serviceProvider;
            _connectionRouter = connectionRouter;
            Console.CancelKeyPress += Console_CancelKeyPress;
        }

        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            _quitEvent.Set();
            e.Cancel = true;
        }

        public void Startup()
        {
            _connectionRouter.Startup();
            _quitEvent.WaitOne();
        }
    }
}
