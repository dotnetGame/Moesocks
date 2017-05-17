using System;
using System.Collections.Generic;
using System.Text;

namespace Moesocks.Client.Services
{
    public class ThroughputEventArgs : EventArgs
    {
        public DateTime DateTime { get; }
        public double Throughput { get; }

        public ThroughputEventArgs(DateTime dateTime, double throughput)
        {
            DateTime = dateTime;
            Throughput = throughput;
        }
    }

    public interface IPerformanceDiagnose
    {
        event EventHandler<ThroughputEventArgs> SendingThroughputCollected;
        event EventHandler<ThroughputEventArgs> ReceivingThroughputCollected;
    }
}
