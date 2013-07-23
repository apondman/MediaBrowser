using System;
namespace Pondman.MediaPortal
{
    public interface ILogger
    {
        void Debug(string format, params object[] args);
        void Error(Exception e);
        void Error(string format, params object[] args);
        void Info(string format, params object[] args);
        void Warn(string format, params object[] args);
    }
}
