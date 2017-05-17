using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Moesocks.Client.Services.Network
{
    class PerformanceDiagnose : IPerformanceDiagnose
    {
        public event EventHandler<ThroughputEventArgs> SendingThroughputCollected;
        public event EventHandler<ThroughputEventArgs> ReceivingThroughputCollected;

        private long _sendThroughputS100, _receiveThroughput100;
        private readonly Timer _timer;
        public static PerformanceDiagnose Current { get; } = new PerformanceDiagnose();

        public PerformanceDiagnose()
        {
            _timer = new Timer(OnTimerTick, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        public void NotifySend(uint throughputScale100)
        {
            Interlocked.Add(ref _sendThroughputS100, throughputScale100);
        }

        public void NotifyReceive(uint throughputScale100)
        {
            Interlocked.Add(ref _receiveThroughput100, throughputScale100);
        }

        private void OnTimerTick(object state)
        {
            var sendLen = Interlocked.Exchange(ref _sendThroughputS100, 0);
            var recvLen = Interlocked.Exchange(ref _receiveThroughput100, 0);

            var time = DateTime.Now;
            SendingThroughputCollected?.Invoke(this, new ThroughputEventArgs(time, sendLen / 100.0));
            ReceivingThroughputCollected?.Invoke(this, new ThroughputEventArgs(time, recvLen / 100.0));
        }
    }
}
