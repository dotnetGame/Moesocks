using Caliburn.Micro;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;

namespace Moesocks.Client.Logging
{
    class FlowDocumentLoggerProvider : ILoggerProvider
    {
        public event EventHandler Added;
        public Paragraph Paragraph { get; } = new Paragraph();
        private readonly Stopwatch _watch = new Stopwatch();

        public FlowDocumentLoggerProvider()
        {
            _watch.Start();
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new Logger(categoryName, this);
        }

        private const int _maxInlines = 1024 * 4;

        private void Log<TState>(string categoryName, LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Execute.BeginOnUIThread(() =>
            {
                var color = LogLevelToBrush(logLevel);
                var inlines = new Inline[]
                {
                    new Run($"[{_watch.Elapsed.ToString(@"hh\:mm\:ss")}] {LogLevelToString(logLevel)}:\t{categoryName}[{eventId}]")
                    {
                        Foreground = color
                    },
                    new LineBreak(),
                    new Run($"\t\t{formatter(state, exception)}")
                    {
                        Foreground = color
                    },
                    new LineBreak()
                };
                Paragraph.Inlines.AddRange(inlines);
                if(Paragraph.Inlines.Count > _maxInlines)
                {
                    for (int i = 0; i < 4 && Paragraph.Inlines.Count != 0; i++)
                        Paragraph.Inlines.Remove(Paragraph.Inlines.FirstInline);
                }
                Added?.Invoke(this, EventArgs.Empty);
            });
        }

        private string LogLevelToString(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    return "trace";
                case LogLevel.Debug:
                    return "debug";
                case LogLevel.Information:
                    return "info ";
                case LogLevel.Warning:
                    return "warn ";
                case LogLevel.Error:
                    return "error";
                case LogLevel.Critical:
                    return "critc";
                case LogLevel.None:
                    return " none";
                default:
                    return "     ";
            }
        }

        private static Brush LogLevelToBrush(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    return Brushes.White;
                case LogLevel.Debug:
                    return Brushes.White;
                case LogLevel.Information:
                    return Brushes.Green;
                case LogLevel.Warning:
                    return Brushes.Yellow;
                case LogLevel.Error:
                    return Brushes.Red;
                case LogLevel.Critical:
                    return Brushes.DarkRed;
                default:
                    return Brushes.White;
            }
        }

        public void Dispose()
        {
            Execute.OnUIThread(() =>
            {
                Paragraph.Inlines.Clear();
                _watch.Stop();
            });
        }

        class Logger : ILogger
        {
            private readonly string _categoryName;
            private readonly FlowDocumentLoggerProvider _provider;

            public Logger(string categoryName, FlowDocumentLoggerProvider provider)
            {
                _categoryName = categoryName;
                _provider = provider;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return new Scope();
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return logLevel >= LogLevel.Information;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (IsEnabled(logLevel))
                    _provider.Log(_categoryName, logLevel, eventId, state, exception, formatter);
            }

            class Scope : IDisposable
            {
                public void Dispose()
                {

                }
            }
        }
    }
}
