using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Moesocks.Client
{
    class ProductInformation : IProductInformation
    {
        public Version ProductVersion { get; }

        public string ProductName { get; }

        public ProductInformation()
        {
            var assembly = typeof(ProductInformation).Assembly;

            ProductVersion = assembly.GetName().Version;
            ProductName = assembly.GetCustomAttribute<AssemblyProductAttribute>().Product;
        }
    }
}
