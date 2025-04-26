using System;

namespace Paging
{
    public static class Logger
    {
        private static readonly Lazy<ILogger> defaultLogger = new Lazy<ILogger>(CreateDefaultLogger, System.Threading.LazyThreadSafetyMode.PublicationOnly);
        private static ILogger logger;

        private static ILogger CreateDefaultLogger()
        {
            return new NullLogger();
        }

        public static void SetLogger(ILogger logger)
        {
            Logger.logger = logger;
        }
    
        public static ILogger Current => Logger.logger ?? defaultLogger.Value;

        public static void Debug(string message)
        {
            Current.Log(LogLevel.Debug, message);
        }

        public static void Info(string message)
        {
            Current.Log(LogLevel.Info, message);
        }

        public static void Warning(string message)
        {
            Current.Log(LogLevel.Warning, message);
        }

        public static void Error(string message, Exception ex)
        {
            Current.Log(LogLevel.Error, message + $" {ex.Message} {Environment.NewLine} {ex.StackTrace}");
        }
    }
}
