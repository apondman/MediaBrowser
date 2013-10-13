using System.Runtime.Serialization;

namespace Pondman.MediaPortal.MediaBrowser
{
    [DataContract(Name = "ViewSettings", Namespace = "http://mediabrowser3.com/settings/view")]
    public class MediaBrowserViewSettings
    {
        public MediaBrowserViewSettings(string userId)
        {

        }

        [DataMember()]
        public string Id { get; private set; }

        [DataMember()]
        public string Layout { get; set; }
    }
}