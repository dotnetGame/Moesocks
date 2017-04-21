using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Moesocks.Client.Views;

namespace Moesocks.Client.ViewModels
{
    class SystemTrayIconViewModel : ViewAware
    {
        public void Exit()
        {
            Application.Current.Shutdown();
        }

        public void DisplayMainWindow()
        {
            if (!Application.Current.Windows.OfType<ShellView>().Any())
                IoC.Get<IWindowManager>().ShowWindow(IoC.Get<IShell>());
        }
    }
}
