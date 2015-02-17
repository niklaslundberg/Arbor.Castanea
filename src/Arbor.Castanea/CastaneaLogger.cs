using System;

namespace Arbor.Castanea
{
    public static class CastaneaLogger
    {
        static Action<string> _log;
        static Action<string> _errorLog;
        static Action<string> _debugLog;

        public static Action<string> Log
        {
            get { return _log; }
        }

        public static Action<string> ErrorLog
        {
            get { return _errorLog; }
        }

        public static Action<string> DebugLog
        {
            get { return _debugLog; }
        }

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
            if (_log != null)
            {
                _log(message);
            }
        }

        public static void WriteDebug(string message)
        {
            if (_debugLog != null)
            {
                _debugLog(message);
            }
        }

        public static void WriteError(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (_errorLog != null)
            {
                _errorLog(message);
            }
        }
    }
}