using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using MediaPortal.Profile;

namespace Pondman.MediaPortal.MediaBrowser
{
    [DataContract(Name = "UserSettings", Namespace = "http://mediabrowser3.com/settings/profile")]
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
        public string PasswordHash { get; private set; }

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
        /// <param name="hash">The password hash.</param>
        public void RememberAuth(string hash)
        {
            RememberMe = true;
            PasswordHash = hash;
        }

        /// <summary>
        /// Forgets the authentication for this profile.
        /// </summary>
        public void ForgetAuth()
        {
            RememberMe = null;
            PasswordHash = null;
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