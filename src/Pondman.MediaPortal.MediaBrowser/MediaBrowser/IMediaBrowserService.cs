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
        /// Occurs when system information changes.
        /// </summary>
        event  EventHandler<SystemInfoChangedEventArgs> SystemInfoChanged;

        /// <summary>
        /// Gets the plugin associated with this service.
        /// </summary>
        /// <value>
        /// The plugin.
        /// </value>
        MediaBrowserPlugin Plugin { get; }

        /// <summary>
        /// Gets the default Media Browser client.
        /// </summary>
        /// <value>
        /// The client.
        /// </value>
        MediaBrowserClient Client { get; }

        /// <summary>
        /// Gets or sets the endpoint of the server.
        /// </summary>
        /// <value>
        /// The server endpoint.
        /// </value>
        ServerDiscoveryInfo Server { get; set; }

        /// <summary>
        /// Gets a value indicating whether the media browser server has been located.
        /// </summary>
        /// <value>
        /// <c>true</c> if the server is known; otherwise, <c>false</c>.
        /// </value>
        bool IsServerLocated { get; }

        /// <summary>
        /// Gets or sets the server info.
        /// </summary>
        /// <value>
        /// The server info.
        /// </value>
        PublicSystemInfo System { get; }

        /// <summary>
        /// Locates the MediaBrowser server on the network.
        /// </summary>
        /// <param name="retryIntervalMs">retry interval in milliseconds.</param>
        void Discover(int retryIntervalMs = 60000);

        /// <summary>
        /// Updates the media browser system info.
        /// </summary>
        void Update();
    }

    
}
