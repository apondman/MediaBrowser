using System;
using MediaBrowser.Model.System;

namespace Pondman.MediaPortal.MediaBrowser.Events
{
    public class SystemInfoChangedEventArgs : EventArgs
    {
        private readonly SystemInfo _systemInfo;

        public SystemInfoChangedEventArgs(SystemInfo info)
        {
            _systemInfo = info;
        }

        /// <summary>
        /// Gets the system information.
        /// </summary>
        /// <value>
        /// The system information.
        /// </value>
        public SystemInfo SystemInfo
        {
            get
            {
                return _systemInfo;
            }
        }
    }
}
