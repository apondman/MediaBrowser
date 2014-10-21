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
            ServerCredentials = new ServerCredentials();
            UserConfiguration = new HashSet<MediaBrowserUserSettings>();
        }

        public ServerCredentials ServerCredentials { get; set; }

        public HashSet<MediaBrowserUserSettings> UserConfiguration { get; set; }
        
    }
}
