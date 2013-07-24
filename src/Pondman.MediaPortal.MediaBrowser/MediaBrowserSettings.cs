﻿using MediaPortal.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Pondman.MediaPortal.MediaBrowser
{
    /// <summary>
    /// Temp class for settings to refactored later
    /// </summary>
    public class MediaBrowserSettings
    {
        ILogger _logger;
        
        public MediaBrowserSettings(ILogger logger = null)
        {
            _logger = logger ?? NullLogger.Instance;

            MediaCacheFolder = Path.Combine(Config.GetFolder(Config.Dir.Thumbs), MediaBrowserPlugin.DefaultName);
            ShowRandomBackdrop = true;
            LogProperties = true; // todo: change to false on release
            DisplayName = MediaBrowserPlugin.DefaultName;
        }


        /// <summary>
        /// Gets or sets the display name for the plugin.
        /// </summary>
        /// <value>
        /// The display name.
        /// </value>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the media cache folder.
        /// </summary>
        /// <value>
        /// The media cache folder.
        /// </value>
        public string MediaCacheFolder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether randomized backdrops are used when viewing items
        /// </summary>
        /// <value>
        ///   <c>true</c> if randomized otherwise, <c>false</c>.
        /// </value>
        public bool ShowRandomBackdrop { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to log skin properties.
        /// </summary>
        /// <value>
        ///   <c>true</c> if properties should be logged otherwise, <c>false</c>.
        /// </value>
        public bool LogProperties { get; set; }
    }
}