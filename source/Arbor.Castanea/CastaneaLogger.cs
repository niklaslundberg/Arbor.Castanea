using System;

namespace Arbor.Castanea
{
    public static class CastaneaLogger
    {
        static Action<string> log;
        static Action<string> errorLog;

        public static void SetLoggerAction(Action<string> loggerAction)
        {
            log = loggerAction;
        }
        public static void SetErrorLoggerAction(Action<string> loggerAction)
        {
            errorLog = loggerAction;
        }

        public static void Write(string message)
        {
            if (log != null)
            {
                log(message);
            }
        }

        public static void WriteError(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (errorLog != null)
            {
                errorLog(message);
            }
        }
    }
}