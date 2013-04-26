using System;

namespace Arbor.Castanea
{
	public static class CastaneaLogger
	{
		static Action<string> log;

		public static void SetLoggerAction(Action<string> loggerAction)
		{
			log = loggerAction;
		}

		public static void Write(string message)
		{
			if (log != null)
			{
				log(message);
			}
		}
	}
}