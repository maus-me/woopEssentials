using System;
using Microsoft.Extensions.Logging;
using Vintagestory.API.Server;

namespace Th3Essentials.Discord
{
    internal class Th3LoggerFactory : ILoggerFactory
    {
        private bool _disposed = false;

        private readonly ICoreServerAPI _api;

        private readonly LogLevel _logLevel;

        public Th3LoggerFactory(ICoreServerAPI api, LogLevel logLevel)
        {
            _api = api;
            _logLevel = logLevel;
        }

        public void AddProvider(ILoggerProvider provider)
        {
            throw new InvalidOperationException("This logger does not allow to add providers.");
        }

        public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
        {
            return _disposed ? throw new InvalidOperationException("This logger factory has been disposed.") : new Th3Logger(_api, _logLevel);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }
    }
}