using Caliburn.Micro;
using LiveCharts;
using LiveCharts.Defaults;
using Moesocks.Client.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moesocks.Client.Areas.Pages.ViewModels
{
    class PerformanceViewModel : PropertyChangedBase
    {
        public string Title => "性能";

        public ChartValues<DateTimePoint> InboundValues { get; }
        public ChartValues<DateTimePoint> OutboundValues { get; }
        public Func<double, string> DateTimeFormatter { get; } = value => new DateTime((long)value).ToString("mm:ss");

        private double _axisMin;
        public double AxisMin
        {
            get => _axisMin;
            set
            {
                _axisMin = value;
                NotifyOfPropertyChange(nameof(AxisMin));
            }
        }

        private double _axisMax;
        public double AxisMax
        {
            get => _axisMax;
            set
            {
                _axisMax = value;
                NotifyOfPropertyChange(nameof(AxisMax));
            }
        }

        public double AxisStep { get; }
        public double AxisUnit { get; }

        public PerformanceViewModel(IPerformanceDiagnose performanceDiagnose)
        {
            InboundValues = new ChartValues<DateTimePoint>();
            OutboundValues = new ChartValues<DateTimePoint>();

            //AxisStep forces the distance between each separator in the X axis
            AxisStep = TimeSpan.FromSeconds(1).Ticks;
            //AxisUnit forces lets the axis know that we are plotting seconds
            //this is not always necessary, but it can prevent wrong labeling
            AxisUnit = TimeSpan.TicksPerSecond;

            SetAxisLimits(DateTime.Now);

            performanceDiagnose.SendingThroughputCollected += PerformanceDiagnose_SendingThroughputCollected;
            performanceDiagnose.ReceivingThroughputCollected += PerformanceDiagnose_ReceivingThroughputCollected;
        }

        private async void PerformanceDiagnose_ReceivingThroughputCollected(object sender, ThroughputEventArgs e)
        {
            await Execute.OnUIThreadAsync(() =>
            {
                var point = new DateTimePoint
                {
                    DateTime = e.DateTime,
                    Value = e.Throughput
                };

                InboundValues.Add(point);
                if (InboundValues.Count > 10)
                    InboundValues.RemoveAt(0);
                SetAxisLimits(e.DateTime);
            });
        }

        private async void PerformanceDiagnose_SendingThroughputCollected(object sender, ThroughputEventArgs e)
        {
            await Execute.OnUIThreadAsync(() =>
            {
                var point = new DateTimePoint
                {
                    DateTime = e.DateTime,
                    Value = e.Throughput
                };

                OutboundValues.Add(point);
                if (OutboundValues.Count > 10)
                    OutboundValues.RemoveAt(0);
            });
        }

        private void SetAxisLimits(DateTime dateTime)
        {
            AxisMax = dateTime.Ticks + TimeSpan.FromSeconds(1).Ticks; // lets force the axis to be 1 second ahead
            AxisMin = dateTime.Ticks - TimeSpan.FromSeconds(8).Ticks; // and 8 seconds behind
        }
    }
}
