using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Moesocks.Client.Services
{
    public class NewReleaseFoundEventArgs : EventArgs
    {
        public string Version { get; }
        public FileInfo UpdatePack { get; }

        public NewReleaseFoundEventArgs(string version, FileInfo updatePack)
        {
            Version = version;
            UpdatePack = updatePack;
        }
    }

    public interface IUpdateService
    {
        event EventHandler<NewReleaseFoundEventArgs> NewReleaseFound;

        void Startup();
    }
}
