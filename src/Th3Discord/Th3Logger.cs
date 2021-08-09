using System;
using Microsoft.Extensions.Logging;
using Vintagestory.API.Server;

namespace Th3Essentials.Discord
{
    internal class Th3Logger : ILogger<Th3Essentials>
    {
        private static readonly object _lock = new object();

        private readonly ICoreServerAPI _api;

        private LogLevel MinimumLevel { get; }

        public Th3Logger(ICoreServerAPI api, LogLevel logLevel)
        {
            _api = api;
            MinimumLevel = logLevel;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= MinimumLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, System.Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }
            lock (_lock)
            {
                string message = formatter(state, exception);
                switch (logLevel)
                {
                    case LogLevel.Trace:
                        {
                            _api.Logger.VerboseDebug("{0}", message);
                            break;
                        }
                    case LogLevel.Debug:
                        {
                            _api.Logger.VerboseDebug("{0}", message);
                            break;
                        }
                    case LogLevel.Information:
                        {
                            _api.Logger.Debug("{0}", message);
                            break;
                        }
                    case LogLevel.Warning:
                        {
                            _api.Logger.Warning("{0}", message);
                            break;
                        }
                    case LogLevel.Error:
                        {
                            _api.Logger.Error("{0}", message);
                            break;
                        }
                    case LogLevel.Critical:
                        {
                            _api.Logger.Fatal("{0}", message);
                            break;
                        }
                    case LogLevel.None:
                        break;
                }
            }
        }
    }
}