using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Moesocks.Client.Services.Network
{
    interface IMessageBus
    {
        Task SendAsync(uint sessionKey, uint identifier, object message);
        void BeginReceive(uint sessionKey, Action<uint, object> receiver);
        void EndReceive(uint sessionKey);
    }
}
