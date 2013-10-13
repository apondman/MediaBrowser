using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Pondman.MediaPortal.MediaBrowser
{
    [DataContract(Name = "ProfileSettings", Namespace = "http://mediabrowser3.com/settings/profile")]
    public class MediaBrowserProfileSettings
    {
        [DataMember(Name = "ViewData")]
        private List<MediaBrowserViewSettings> _viewData;

        public MediaBrowserProfileSettings(string userId)
        {
            ShowRandomBackdrop = true;
        }

        [DataMember()]
        public string UserId { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether randomized backdrops are used when viewing items
        /// </summary>
        /// <value>
        ///   <c>true</c> if randomized otherwise, <c>false</c>.
        /// </value>
        [DataMember()]
        public bool ShowRandomBackdrop { get; set; }

    }
}