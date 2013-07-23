using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pondman.MediaPortal
{
    public sealed class NullLogger : ILogger
    {
        static readonly ILogger _instance = NullLogger.Instance;

        public static ILogger Instance {
            get 
            {
                return _instance;
            }
        }

        public void Debug(string format, params object[] args)
        {
            return;
        }

        public void Error(Exception e)
        {
            return;
        }

        public void Error(string format, params object[] args)
        {
            return;
        }

        public void Info(string format, params object[] args)
        {
            return;
        }

        public void Warn(string format, params object[] args)
        {
            return;
        }
    }
}
