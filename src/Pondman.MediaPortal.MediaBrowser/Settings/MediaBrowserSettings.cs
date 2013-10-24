using System;
using System.Linq;
using MediaPortal.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace Pondman.MediaPortal.MediaBrowser
{
    /// <summary>
    /// Global and User specific settings.
    /// </summary>
    [DataContract(Name = "MediaBrowserSettings", Namespace = "urn://mediaportal/mb3/settings")]
    public class MediaBrowserSettings
    {

        [DataMember(Name = "UserData")] private HashSet<MediaBrowserUserSettings> _userData;
        [DataMember(Name = "Version")] private string _version;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaBrowserSettings"/> class.
        /// </summary>
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
        [DataMember]
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the media cache folder.
        /// </summary>
        /// <value>
        /// The media cache folder.
        /// </value>
        [DataMember]
        public string MediaCacheFolder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to log skin properties.
        /// </summary>
        /// <value>
        ///   <c>true</c> if properties should be logged otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool LogProperties { get; set; }

        /// <summary>
        /// Gets or sets the default item limit.
        /// </summary>
        /// <value>
        /// The default item limit.
        /// </value>
        [DataMember]
        public int DefaultItemLimit { get; set; }

        /// <summary>
        /// Gets or sets the default user.
        /// </summary>
        /// <value>
        /// The default user.
        /// </value>
        [DataMember]
        public string DefaultUserId { get; private set; }

        /// <summary>
        /// Gets or sets the use default user.
        /// </summary>
        /// <value>
        /// The use default user.
        /// </value>
        [DataMember]
        public bool? UseDefaultUser { get; set; }

        /// <summary>
        /// Sets the default user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        public void SetDefaultUser(string userId)
        {
            UseDefaultUser = true;
            DefaultUserId = userId;
        }

        /// <summary>
        /// Resets the default user.
        /// </summary>
        public void ResetDefaultUser()
        {
            UseDefaultUser = null;
            DefaultUserId = null;
        }

        /// <summary>
        /// Get user specific settings
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns></returns>
        public MediaBrowserUserSettings ForUser(string userId)
        {
            var settings = _userData.FirstOrDefault(x => x.UserId == userId);
            if (settings != null) return settings;
            settings = new MediaBrowserUserSettings(userId);
            _userData.Add(settings);

            return settings;
        }

        /// <summary>
        /// Performs an upgrade if needed when encountering a new version
        /// </summary>
        /// <param name="version">The version.</param>
        public void Upgrade(Version version)
        {
            if (_version == null)
            {
                _version = version.ToString();
                return;
            }
            
            var current = new Version(_version);

            if (current < version)
            {
                _version = version.ToString();
            }
        }

    }
}
