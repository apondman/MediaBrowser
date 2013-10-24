using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Pondman.MediaPortal.MediaBrowser.Events
{
    /// <summary>
    /// Indicates a server changed event
    /// </summary>
    public class ServerChangedEventArgs : EventArgs
    {
        private readonly IPEndPoint _endpoint;

        public ServerChangedEventArgs(IPEndPoint endpoint)
        {
            _endpoint = endpoint;
        }

        /// <summary>
        /// Gets the end point for the server.
        /// </summary>
        /// <value>
        /// The end point.
        /// </value>
        private IPEndPoint EndPoint
        {
            get
            {
                return _endpoint;
            }
        }
    }
}
