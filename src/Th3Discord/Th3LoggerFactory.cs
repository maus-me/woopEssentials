using System;
using Microsoft.Extensions.Logging;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Th3Essentials.Discord
{
    class Th3LoggerFactory : ILoggerFactory
    {
        private Boolean _disposed = false;

        private ICoreServerAPI _api;

        private LogLevel _logLevel;

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
            if (_disposed)
            {
                throw new InvalidOperationException("This logger factory has been disposed.");
            }
            return new Th3Logger(_api, _logLevel);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
        }
    }
}