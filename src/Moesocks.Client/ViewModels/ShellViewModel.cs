using Caliburn.Micro;
using Moesocks.Client.Areas.Pages.ViewModels;
using System.Windows.Controls;

namespace Moesocks.Client.ViewModels
{
    public class ShellViewModel : PropertyChangedBase, IShell
    {
        public object[] Pages { get; }

        public ShellViewModel()
        {
            Pages = new object[]
            {
                IoC.Get<LoggingViewModel>(),
                IoC.Get<PerformanceViewModel>(),
                IoC.Get<SettingsViewModel>()
            };
        }
    }
}