using System;
using System.Collections.Generic;
using System.Text;

namespace Moesocks.Client
{
    public interface IProductInformation
    {
        string ProductName { get; }
        Version ProductVersion { get; }
    }
}
