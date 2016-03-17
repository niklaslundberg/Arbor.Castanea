using System;

namespace Arbor.Castanea
{
    public static class CastaneaLogger
    {
        static Action<string> _log;
        static Action<string> _errorLog;
        static Action<string> _debugLog;

        public static Action<string> Log => _log;

        public static Action<string> ErrorLog => _errorLog;

        public static Action<string> DebugLog => _debugLog;

        public static void SetLoggerAction(Action<string> loggerAction)
        {
            _log = loggerAction;
        }

        public static void SetLoggerDebugAction(Action<string> debugAction)
        {
            _debugLog = debugAction;
        }

        public static void SetErrorLoggerAction(Action<string> loggerAction)
        {
            _errorLog = loggerAction;
        }

        public static void Write(string message)
        {
            _log?.Invoke(message);
        }

        public static void WriteDebug(string message)
        {
            _debugLog?.Invoke(message);
        }

        public static void WriteError(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            _errorLog?.Invoke(message);
        }
    }
}