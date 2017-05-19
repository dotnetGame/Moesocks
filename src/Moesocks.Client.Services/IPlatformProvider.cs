using System;
using System.Collections.Generic;
using System.Text;

namespace Moesocks.Client.Services
{
    public interface IPlatformProvider
    {
        void SetProxy(string proxy);
        void UnsetProxy();
    }
}
