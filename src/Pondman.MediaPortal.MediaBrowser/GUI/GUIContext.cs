using MediaBrowser.Model.Dto;
using MediaBrowser.Model.System;
using MediaPortal.GUI.Library;
using MediaPortal.Services;
using Pondman.MediaPortal.MediaBrowser.Events;

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

        /// <summary>
        /// Gets a value indicating whether there is an active user.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has active user; otherwise, <c>false</c>.
        /// </value>
        public bool HasActiveUser
        {
            get
            {
                return (ActiveUser != _anonymousUser);
            }
        }
        
        public UserDto ActiveUser
        {
            get
            {
                if (Client == null || Client.CurrentUser == null) return _anonymousUser;

                return Client.CurrentUser;
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
                if (_service != null || !GlobalServiceProvider.IsRegistered<IMediaBrowserService>()) return _service;
                _service = GlobalServiceProvider.Get<IMediaBrowserService>();
                return _service;
            }
        } IMediaBrowserService _service;

        public static void OnSystemInfoChanged(object sender, SystemInfoChangedEventArgs changed)
        {
            changed.SystemInfo.Publish(MediaBrowserPlugin.DefaultProperty + ".System");
        } 

        public MediaBrowserClient Client
        {
            get
            {
                // todo: later use GUI specific client instead of the default service client
                return (Service != null) ? Service.Client : null;
            }
        }

        public void Update(BaseItemDto item, string context = "")
        {
            Update(item.Type, item.Id, item.Name, context);
        }

        public void Update(string itemType, string itemId, string itemName, string context = "")
        {
            if (Client != null && Client.WebSocketConnection != null)
            {
                if (itemType == "View")
                {
                    itemType = "";
                    itemId = "";
                }

                Client.WebSocketConnection.SendContextMessage(itemType, itemId, itemName, context, MediaBrowserPlugin.Log.Error);
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
