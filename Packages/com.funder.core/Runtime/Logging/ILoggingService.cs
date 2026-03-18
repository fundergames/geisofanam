namespace Funder.Core.Logging
{
    public interface ILoggingService
    {
        void Debug(string category, string message);
        void Info(string category, string message);
        void Warning(string category, string message);
        void Error(string category, string message);
    }
}
