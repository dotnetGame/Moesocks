using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Moesocks.Client.Services.Configuration
{
    [ConfigurationSectionName("update")]
    public class UpdateConfiguration : ConfigurationBase, IOptions<UpdateConfiguration>
    {
        private bool _enabled = true;
        public bool Enabled
        {
            get => _enabled;
            set => SetProperty(ref _enabled, value);
        }

        private bool _useByteBasisNetwork;
        public bool UseByteBasisNetwork
        {
            get => _useByteBasisNetwork;
            set => SetProperty(ref _useByteBasisNetwork, value);
        }

        UpdateConfiguration IOptions<UpdateConfiguration>.Value => this;
    }
}
