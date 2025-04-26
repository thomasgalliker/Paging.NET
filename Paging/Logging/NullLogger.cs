namespace Paging
{
    public class NullLogger : ILogger
    {
        public void Log(LogLevel level, string message)
        {
        }
    }
}