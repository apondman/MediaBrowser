﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.System;
using System.Net;

namespace Pondman.MediaPortal.MediaBrowser
{
    public interface IMediaBrowserService
    {
        /// <summary>
        /// Occurs when the server changes.
        /// </summary>
        event Action<IPEndPoint> ServerChanged;
        
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
        IPEndPoint Server { get; set; }

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
        SystemInfo System { get; }

        /// <summary>
        /// Locate Media Browser server on the network.
        /// </summary>
        void Discover();

        /// <summary>
        /// Updates the media browser system info.
        /// </summary>
        void Update();
    }
}