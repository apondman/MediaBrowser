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
        private List<MediaBrowserProfileSettings> _userData;
        
        public MediaBrowserSettings()
        {
            MediaCacheFolder = Path.Combine(Config.GetFolder(Config.Dir.Thumbs), MediaBrowserPlugin.DefaultName);
            LogProperties = true; // todo: change to false on release
            DisplayName = MediaBrowserPlugin.DefaultName;
            DefaultItemLimit = 50;

            _userData = new List<MediaBrowserProfileSettings>();
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
        public string DefaultUser { get; set; }

    }
}
