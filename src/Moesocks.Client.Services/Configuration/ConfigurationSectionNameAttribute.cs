using System;
using System.Collections.Generic;
using System.Text;

namespace Moesocks.Client.Services.Configuration
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ConfigurationSectionNameAttribute : Attribute
    {
        public string SectionName { get; }

        public ConfigurationSectionNameAttribute(string sectionName)
        {
            SectionName = sectionName;
        }
    }
}
