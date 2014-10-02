using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using MediaPortal.Profile;
using Newtonsoft.Json;

namespace Pondman.MediaPortal.MediaBrowser
{
    public class MediaBrowserUserSettings : IEquatable<MediaBrowserUserSettings>
    {
        [JsonProperty]
        private HashSet<MediaBrowserContextSettings> _contextData;

        public MediaBrowserUserSettings(string userId)
        {
            UserId = userId;
            ShowRandomBackdrop = true;
            _contextData = new HashSet<MediaBrowserContextSettings>();
        }

        public string UserId { get; set; }

        public string Password { get; set; }

        public bool? RememberMe { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether randomized backdrops are used when viewing items
        /// </summary>
        /// <value>
        ///   <c>true</c> if randomized otherwise, <c>false</c>.
        /// </value>
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

            if (contextId == null) return settings;
            
            _contextData.Add(settings);

            return settings;
        }

        public bool Equals(MediaBrowserUserSettings other)
        {
            return this.UserId == other.UserId;
        }
    }
}