using System;
using TerrariaModder.Core.Logging;

namespace MagicStorageTM
{
    /// <summary>Thin static wrapper over the Core per-mod logger.</summary>
    internal static class Log
    {
        private static ILogger _logger;

        public static void Init(ILogger logger) => _logger = logger;

        public static void Debug(string msg) => _logger?.Debug(msg);
        public static void Info(string msg) => _logger?.Info(msg);
        public static void Warn(string msg) => _logger?.Warn(msg);
        public static void Error(string msg) => _logger?.Error(msg);
        public static void Error(string msg, Exception ex) => _logger?.Error($"{msg}: {ex}");
    }
}
