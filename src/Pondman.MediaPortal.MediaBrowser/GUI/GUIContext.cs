using MediaBrowser.Model.Dto;
using MediaPortal.Services;

namespace Pondman.MediaPortal.MediaBrowser.GUI
{
    public sealed class GUIContext
    {
        readonly UserDto _anonymousUser = new UserDto { Id = "user/anonymous", Name = MediaBrowserPlugin.UI.Resource.Anonymous };
        
        #region Singleton

        static readonly GUIContext instance = new GUIContext();
        
        static GUIContext()
        {
        }

        GUIContext()
        {
        }       

        public static GUIContext Instance
        {
            get
            {
                return instance;
            }
        }

        #endregion

        public UserDto ActiveUser
        {
            get
            {
                return Client.CurrentUser ?? _anonymousUser;
            }
            set
            {
                Client.CurrentUser = value;
            }
        } 

        public IMediaBrowserService Service
        {
            get
            {
                if (_service == null)
                {
                    if (GlobalServiceProvider.IsRegistered<IMediaBrowserService>())
                    {
                        _service = GlobalServiceProvider.Get<IMediaBrowserService>();
                    }
                }

                return _service;
            }
        } IMediaBrowserService _service;

        public MediaBrowserClient Client
        {
            get
            {
                // todo: later use GUI specific client instead of the default service client
                return (Service != null) ? Service.Client : null;
            }
        }

        public bool IsServerReady
        {
            get
            {
                // implementation
                if (Service == null)
                {
                    MediaBrowserPlugin.Log.Error("MediaBrowserService not found.");
                    return false;
                }

                if (!Service.IsServerLocated)
                {
                    // todo: show discover dialog
                    MediaBrowserPlugin.Log.Error("MediaBrowser server not available.");
                    return false;
                }

                return true;
            }
        }

        public void PublishUser()
        {
            GUICommon.UserPublishWorker.BeginInvoke(GUIContext.Instance.ActiveUser, GUICommon.UserPublishWorker.EndInvoke, null);
        }        

    }
}
