﻿using System.Linq;
using MediaPortal.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace Pondman.MediaPortal.MediaBrowser
{
    /// <summary>
    /// Temp class for settings to refactored later
    /// </summary>
    [DataContract(Name = "MediaBrowserSettings", Namespace = "http://mediabrowser3.com/settings")]
    public class MediaBrowserSettings
    {
       
        [DataMember(Name = "UserData")]
        private HashSet<MediaBrowserUserSettings> _userData;
        
        public MediaBrowserSettings()
        {
            MediaCacheFolder = Path.Combine(Config.GetFolder(Config.Dir.Thumbs), MediaBrowserPlugin.DefaultName);
            LogProperties = true; // todo: change to false on release
            DisplayName = MediaBrowserPlugin.DefaultName;
            DefaultItemLimit = 50;

            _userData = new HashSet<MediaBrowserUserSettings>();
        }

        /// <summary>
        /// Gets or sets the display name for the plugin.
        /// </summary>
        /// <value>
        /// The display name.
        /// </value>
        [DataMember()]
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the media cache folder.
        /// </summary>
        /// <value>
        /// The media cache folder.
        /// </value>
        [DataMember()]
        public string MediaCacheFolder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to log skin properties.
        /// </summary>
        /// <value>
        ///   <c>true</c> if properties should be logged otherwise, <c>false</c>.
        /// </value>
        [DataMember()]
        public bool LogProperties { get; set; }

        /// <summary>
        /// Gets or sets the default item limit.
        /// </summary>
        /// <value>
        /// The default item limit.
        /// </value>
        [DataMember()]
        public int DefaultItemLimit { get; set; }

        /// <summary>
        /// Gets or sets the default user.
        /// </summary>
        /// <value>
        /// The default user.
        /// </value>
        [DataMember()]
        public string DefaultUserId { get; set; }

        /// <summary>
        /// Gets or sets the use default user.
        /// </summary>
        /// <value>
        /// The use default user.
        /// </value>
        [DataMember()]
        public bool? UseDefaultUser { get; set; }

        public MediaBrowserUserSettings ForUser(string userId)
        {
            var settings = _userData.FirstOrDefault(x => x.UserId == userId);
            if (settings != null) return settings;
            settings = new MediaBrowserUserSettings(userId);
            _userData.Add(settings);

            return settings;
        }

    }
}