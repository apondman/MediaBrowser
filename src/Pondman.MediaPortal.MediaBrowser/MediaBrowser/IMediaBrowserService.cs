using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.System;
using System.Net;
using Pondman.MediaPortal.MediaBrowser.Events;
using MediaBrowser.Model.ApiClient;

namespace Pondman.MediaPortal.MediaBrowser
{
    public interface IMediaBrowserService : IDisposable
    {
        /// <summary>
        /// Gets the plugin associated with this service.
        /// </summary>
        /// <value>
        /// The plugin.
        /// </value>
        MediaBrowserPlugin Plugin { get; }

        /// <summary>
        /// Gets the media browser connection manager.
        /// </summary>
        /// <value>
        /// The connection manager.
        /// </value>
        IConnectionManager ConnectionManager { get; }
    }
}
