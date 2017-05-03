using Caliburn.Micro;
using Microsoft.Extensions.Options;
using Moesocks.Client.Services;
using Moesocks.Client.Services.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moesocks.Client.Areas.Pages.ViewModels
{
    class SettingsViewModel : PropertyChangedBase
    {
        public string Title => "设置";

        public UpdateConfiguration Update { get; }
        public IProductInformation ProductInformation { get; }

        public SettingsViewModel(IOptions<UpdateConfiguration> updateConfig, IProductInformation productInfo)
        {
            Update = updateConfig.Value;
            ProductInformation = productInfo;
        }

        public void CheckUpdate()
        {
            IoC.Get<IUpdateService>().Startup();
        }
    }
}
