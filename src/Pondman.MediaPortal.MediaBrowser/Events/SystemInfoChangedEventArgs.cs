using System;
using MediaBrowser.Model.System;

namespace Pondman.MediaPortal.MediaBrowser.Events
{
    public class SystemInfoChangedEventArgs : EventArgs
    {
        private readonly PublicSystemInfo _systemInfo;

        public SystemInfoChangedEventArgs(PublicSystemInfo info)
        {
            _systemInfo = info;
        }

        /// <summary>
        /// Gets the system information.
        /// </summary>
        /// <value>
        /// The system information.
        /// </value>
        public PublicSystemInfo SystemInfo
        {
            get
            {
                return _systemInfo;
            }
        }
    }
}
