using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Moesocks.Client.ViewModels
{
    class SystemTrayIconViewModel : ViewAware
    {

        public void Exit()
        {
            Application.Current.Shutdown();
        }
    }
}
