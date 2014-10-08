using MediaBrowser.ApiInteraction;
using MediaBrowser.Model.ApiClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pondman.MediaPortal.MediaBrowser
{
    public class MediaBrowserServerSettings
    {
        public MediaBrowserServerSettings() 
        {
            CredentialConfiguration = new ServerCredentialConfiguration();
            UserConfiguration = new HashSet<MediaBrowserUserSettings>();
        }

        public ServerCredentialConfiguration CredentialConfiguration { get; set; }

        public HashSet<MediaBrowserUserSettings> UserConfiguration { get; set; }
        
    }
}
