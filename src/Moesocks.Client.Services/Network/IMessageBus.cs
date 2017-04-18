using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Moesocks.Client.Services.Network
{
    interface IMessageBus
    {
        Task SendAsync(uint sessionKey, uint identifier, object message);
        Task<(uint identifier, object message)> ReceiveAsync(uint sessionKey);
    }
}
