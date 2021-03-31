namespace Netch.Interfaces
{
    public interface ILogger
    {
        void Info(string text);
        void Warning(string text);
        void Error(string text);
        void Debug(string s);
        void ShowLog();
    }
}