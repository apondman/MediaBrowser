using MediaPortal.GUI.Library;
using System;

namespace Pondman.MediaPortal
{
    /// <summary>
    /// Wrapper for MediaPortal logger
    /// </summary>
    public class Logger : Pondman.MediaPortal.ILogger
    {
        readonly string _prefix;

        public Logger(string prefix)
        {
            _prefix = prefix + ": ";
        }

        public void Info(string format, params object[] args)
        {
            Log.Info(_prefix + format, args);
        }

        public void Warn(string format, params object[] args)
        {
            Log.Warn(_prefix + format, args);
        }

        public void Debug(string format, params object[] args)
        {
            Log.Debug(_prefix + format, args);
        }

        public void Error(string format, params object[] args)
        {
            Log.Error(_prefix + format, args);
        }

        public void Error(Exception e)
        {
            Log.Error(e);
        }
    }
    
}
