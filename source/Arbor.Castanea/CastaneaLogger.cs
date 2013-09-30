using System;

namespace Arbor.Castanea
{
    public static class CastaneaLogger
    {
        static Action<string> _log;
        static Action<string> _errorLog;

        public static void SetLoggerAction(Action<string> loggerAction)
        {
            _log = loggerAction;
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