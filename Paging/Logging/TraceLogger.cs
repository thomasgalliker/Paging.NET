using System;

namespace Paging
{
    public class TraceLogger : ILogger
    {
        public void Log(LogLevel level, string message)
        {
            System.Diagnostics.Debug.WriteLine($"{DateTime.UtcNow}|Paging.NET|{level}|{message}[EOL]");
        }
    }
}