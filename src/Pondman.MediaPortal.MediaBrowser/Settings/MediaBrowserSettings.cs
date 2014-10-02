using MediaPortal.Configuration;

namespace Pondman.MediaPortal.MediaBrowser
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;

    public class MediaBrowserSettings : ISettings
    {
        private HashSet<MediaBrowserUserSettings> _userData;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaBrowserSettings"/> class.
        /// </summary>
        public MediaBrowserSettings()
        {
            MediaCacheFolder = Path.Combine(Config.GetFolder(Config.Dir.Thumbs), MediaBrowserPlugin.DefaultName);
            DataCacheFolder = Config.GetFolder(Config.Dir.Database);
            LogProperties = false;
            DisplayName = MediaBrowserPlugin.DefaultName;
            DefaultItemLimit = 50;
            PublishDelayMs = 250;
            UiUseUniversalBackButton = true;

            _userData = new HashSet<MediaBrowserUserSettings>();
        }

        /// <summary>
        /// Gets or sets the display name for the plugin.
        /// </summary>
        /// <value>
        /// The display name.
        /// </value>
        public string DisplayName { get; set; }

        public string DataCacheFolder { get; set; }

        /// <summary>
        /// Gets or sets the media cache folder.
        /// </summary>
        /// <value>
        /// The media cache folder.
        /// </value>
        public string MediaCacheFolder { get; set; }

        public string LastKnownVersion { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use the BACK button to navigate to parent folder.
        /// </summary>
        /// <value>
        /// <c>true</c> if [UI use universal back button]; otherwise, <c>false</c>.
        /// </value>
        public bool UiUseUniversalBackButton { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to log skin properties.
        /// </summary>
        /// <value>
        ///   <c>true</c> if properties should be logged otherwise, <c>false</c>.
        /// </value>
        public bool LogProperties { get; set; }

        /// <summary>
        /// Gets or sets the default item limit.
        /// </summary>
        /// <value>
        /// The default item limit.
        /// </value>
        public int DefaultItemLimit { get; set; }

        /// <summary>
        /// Gets or sets the default user.
        /// </summary>
        /// <value>
        /// The default user.
        /// </value>
        public string DefaultUserId { get; private set; }

        /// <summary>
        /// Gets or sets the publish delay in milliseconds when traversing a list of items.
        /// </summary>
        /// <value>
        /// publish delay in milliseconds.
        /// </value>
        public int PublishDelayMs { get; set; }

        /// <summary>
        /// Gets or sets the use default user.
        /// </summary>
        /// <value>
        /// The use default user.
        /// </value>
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
            if (LastKnownVersion == null)
            {
                LastKnownVersion = version.ToString();
                return;
            }

            var current = new Version(LastKnownVersion);

            if (current < version)
            {
                // upgrade from 0.13
                if (current.Major == 0 && current.Minor == 13)
                {
                    PublishDelayMs = 250;
                }

                LastKnownVersion = version.ToString();
            }
        }

        public void OnLoad()
        {
            if (!File.Exists(UserDataCache)) return;

            try
            {
                using (StreamReader file = File.OpenText(UserDataCache))
                using (JsonTextReader reader = new JsonTextReader(file))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    _userData = serializer.Deserialize<HashSet<MediaBrowserUserSettings>>(reader);
                }
            }
            catch (Exception e)
            {
                MediaBrowserPlugin.Log.Error("Cannot load user data '" + UserDataCache + "'", e.Message);
            }
        }

        public void OnSave()
        {
            try
            {
                using (FileStream fs = File.Open(UserDataCache, FileMode.Create))
                using (StreamWriter sw = new StreamWriter(fs))
                using (JsonWriter jw = new JsonTextWriter(sw))
                {
                    jw.Formatting = Formatting.Indented;

                    JsonSerializer serializer = new JsonSerializer();

                    serializer.Serialize(jw, _userData);
                }
            }
            catch (Exception e)
            {
                MediaBrowserPlugin.Log.Error("Cannot save user data '" + UserDataCache + "'", e.Message);
            }
        }

        private string UserDataCache
        {
            get
            {
                return Path.Combine(DataCacheFolder, MediaBrowserPlugin.DefaultName + ".json");
            }
        }
    }

    /// <summary>
    /// Global and User specific settings.
    /// </summary>
    [DataContract(Name = "MediaBrowserSettings", Namespace = "urn://mediaportal/mb3/settings")]
    public class MediaBrowserSettingsOld
    {

        [DataMember(Name = "UserData")] private HashSet<MediaBrowserUserSettings> _userData;
        [DataMember(Name = "Version")] private string _version;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaBrowserSettings"/> class.
        /// </summary>
        public MediaBrowserSettingsOld()
        {
            MediaCacheFolder = Path.Combine(Config.GetFolder(Config.Dir.Thumbs), MediaBrowserPlugin.DefaultName);
            LogProperties = false;
            DisplayName = MediaBrowserPlugin.DefaultName;
            DefaultItemLimit = 50;
            PublishDelayMs = 250;
            UiUseUniversalBackButton = true;

            _userData = new HashSet<MediaBrowserUserSettings>();
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
        /// Gets or sets a value indicating whether to use the BACK button to navigate to parent folder.
        /// </summary>
        /// <value>
        /// <c>true</c> if [UI use universal back button]; otherwise, <c>false</c>.
        /// </value>
        public bool UiUseUniversalBackButton { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to log skin properties.
        /// </summary>
        /// <value>
        ///   <c>true</c> if properties should be logged otherwise, <c>false</c>.
        /// </value>
        public bool LogProperties { get; set; }

        /// <summary>
        /// Gets or sets the default item limit.
        /// </summary>
        /// <value>
        /// The default item limit.
        /// </value>
        public int DefaultItemLimit { get; set; }

        /// <summary>
        /// Gets or sets the default user.
        /// </summary>
        /// <value>
        /// The default user.
        /// </value>
        public string DefaultUserId { get; private set; }

        /// <summary>
        /// Gets or sets the publish delay in milliseconds when traversing a list of items.
        /// </summary>
        /// <value>
        /// publish delay in milliseconds.
        /// </value>
        public int PublishDelayMs { get; set; }

        /// <summary>
        /// Gets or sets the use default user.
        /// </summary>
        /// <value>
        /// The use default user.
        /// </value>
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
                // upgrade from 0.13
                if (current.Major == 0 && current.Minor == 13)
                {
                    PublishDelayMs = 250;
                }

                _version = version.ToString();
            }
        }

    }
}
