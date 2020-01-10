using System;
using System.Globalization;
using System.IO;

namespace WebBloatScore
{
    public static class Logger
    {
        private static readonly object locker = new object();
        private static readonly LoggerLevel LogLevel = (LoggerLevel)Enum.Parse(typeof(LoggerLevel), Utilities.LoggerLevel);
        private static readonly string LogFile = Utilities.LoggerFile;

        public static void Error(Exception exception)
        {
            if (exception == null)
                return;

            while (exception.InnerException != null)
                exception = exception.InnerException;

            Error(string.Format(CultureInfo.InvariantCulture, "{0} => {1}\r\n{2}",
                exception.GetType(), exception.Message, exception.StackTrace));
        }

        public static void Error(string message) { Log(LoggerLevel.Error, message); }

        public static void Warning(string message) { Log(LoggerLevel.Warning, message); }

        public static void Info(string message) { Log(LoggerLevel.Info, message); }

        private static void Log(LoggerLevel level, string message)
        {
            if (LogLevel < level)
                return;

            lock (locker)
                File.AppendAllText(LogFile, string.Format(CultureInfo.InvariantCulture, "[{0}] [{1}] - {2}\r\n",
                    level.ToString().ToUpperInvariant(),
                    DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss", CultureInfo.InvariantCulture),
                    message));
        }

        private enum LoggerLevel
        {
            Off = 0,
            Error = 1,
            Warning = 2,
            Info = 3
        }
    }
}