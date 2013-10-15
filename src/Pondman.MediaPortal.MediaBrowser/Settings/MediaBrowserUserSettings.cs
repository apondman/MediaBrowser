using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using MediaPortal.Profile;

namespace Pondman.MediaPortal.MediaBrowser
{
    [DataContract(Name = "UserSettings", Namespace = "urn://mediaportal/mb3/settings/profile")]
    public class MediaBrowserUserSettings : IEquatable<MediaBrowserUserSettings>
    {
        [DataMember(Name = "ContextData")]
        private HashSet<MediaBrowserContextSettings> _contextData;

        public MediaBrowserUserSettings(string userId)
        {
            UserId = userId;
            ShowRandomBackdrop = true;
            _contextData = new HashSet<MediaBrowserContextSettings>();
        }

        [DataMember]
        public string UserId { get; private set; }

        [DataMember]
        public string Password { get; private set; }

        [DataMember]
        public bool? RememberMe { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether randomized backdrops are used when viewing items
        /// </summary>
        /// <value>
        ///   <c>true</c> if randomized otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool ShowRandomBackdrop { get; set; }

        /// <summary>
        /// Remembers the authentication for this profile.
        /// </summary>
        /// <param name="encryptedPassword">The encrypted password.</param>
        public void RememberAuth(string encryptedPassword)
        {
            RememberMe = true;
            Password = encryptedPassword;
        }

        /// <summary>
        /// Forgets the authentication for this profile.
        /// </summary>
        public void ForgetAuth()
        {
            RememberMe = null;
            Password = null;
        }

        public MediaBrowserContextSettings ForContext(string contextId)
        {
            var settings = _contextData.FirstOrDefault(x => x.Context == contextId);
            if (settings != null) return settings;
            settings = new MediaBrowserContextSettings(contextId);
            _contextData.Add(settings);

            return settings;
        }

        public bool Equals(MediaBrowserUserSettings other)
        {
            return this.UserId == other.UserId;
        }
    }
}