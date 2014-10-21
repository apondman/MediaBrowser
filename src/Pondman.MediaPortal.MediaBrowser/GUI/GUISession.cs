using MediaBrowser.ApiInteraction;
using MediaBrowser.Model.ApiClient;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Users;
using MediaPortal.GUI.Library;
using MediaPortal.Services;
using Pondman.MediaPortal.MediaBrowser.Events;
using Pondman.MediaPortal.MediaBrowser.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pondman.MediaPortal.MediaBrowser.GUI
{
    public sealed class GUISession
    {
        readonly UserDto _anonymousUser = new UserDto { Id = "user/anonymous", Name = MediaBrowserPlugin.UI.Resource.Anonymous };
        readonly ILogger _logger;

        #region Singleton

        static readonly GUISession instance = new GUISession(MediaBrowserPlugin.Log);
        
        static GUISession()
        {
            
        }

        GUISession(ILogger logger)
        {
            _logger = logger;
            CurrentUser = _anonymousUser;
            IsConnected = false;
            Service.ConnectionManager.RemoteLoggedOut += OnRemoteLoggedOut;
        }       

        public static GUISession Instance
        {
            get
            {
                return instance;
            }
        }

        #endregion

        public PublicSystemInfo System
        {
            get { return _systemInfo; }
            private set
            {
                _systemInfo = value;

                 // Publish Server Info
                _systemInfo.Publish(MediaBrowserPlugin.DefaultProperty + ".System");
            }
        } PublicSystemInfo _systemInfo;

        public UserDto CurrentUser
        {
            get
            {
                return _currentUser;
            }
            private set
            {
                _currentUser = value;
                PublishUser();
            }
        } UserDto _currentUser;

        public IApiClient Client
        {
            get
            {
                return Service.ConnectionManager.GetApiClient(new BaseItemDto());
            }
        }

        public async Task Connect(CancellationToken? token = null)
        {
            ConnectionResult result = null;

            while (result == null)
            {
                if (MediaBrowserPlugin.Config.Settings.UseServerAddress)
                {
                    _logger.Info("Connecting to: {0}", MediaBrowserPlugin.Config.Settings.ServerAddress);
                    result = await Service.ConnectionManager.Connect(MediaBrowserPlugin.Config.Settings.ServerAddress, token ?? CancellationToken.None);
                }
                else
                {
                    result = await Service.ConnectionManager.Connect(token ?? CancellationToken.None);
                }

                switch (result.State)
                {
                    case ConnectionState.Unavailable:
                        // No servers found. User must manually enter connection info.
                        GUIUtils.ShowOKDialog(MediaBrowserPlugin.UI.Resource.Error, MediaBrowserPlugin.UI.Resource.ServerNotFoundOnTheNetwork);
                        _logger.Error("MediaBrowserService not found.");
                        IsConnected = false;
                        return;
                    case ConnectionState.ServerSelection:
                    // Multiple servers available
                    // Display a selection screen
                        if (result.Servers.Count == 1)
                        {
                            result = await Service.ConnectionManager.Connect(result.Servers[0].LocalAddress, token ?? CancellationToken.None);
                        }
                        break;
                    case ConnectionState.ServerSignIn:
                        // A server was found and the user needs to login.
                        // Display a login screen and authenticate with the server using result.ApiClient
                    case ConnectionState.SignedIn:
                        // A server was found and the user has been signed in using previously saved credentials.
                        // Ready to browse using result.ApiClient    

                        break;
                }
            }

            // set connected
            IsConnected = true;

            // Hook up api event handlers
            var client = result.ApiClient;

            client.MessageCommand += OnSocketMessageCommand;
            client.PlayCommand += OnPlayCommand;
            client.BrowseCommand += OnBrowseCommand;
            client.LibraryChanged += OnLibraryChanged;
            client.Authenticated += OnUserAuthenticated;

            _logger.Info("Found MediaBrowser Server: {0}", client.ServerAddress);

            // Store server
            if (MediaBrowserPlugin.Config.Settings.ServerAddress != client.ServerAddress)
            {
                MediaBrowserPlugin.Config.Settings.ServerAddress = client.ServerAddress;
                MediaBrowserPlugin.Config.Save();
            }

            // Get System Info
            System = await client.GetPublicSystemInfoAsync(token ?? CancellationToken.None);
        }

        private void OnUserAuthenticated(object sender, GenericEventArgs<AuthenticationResult> e)
        {
            CurrentUser = e.Argument.User;
        }

        public async Task Logout()
        {
            _logger.Info("Logging out.");
            
            //_playback.StopAllPlayback();
            await Service.ConnectionManager.Logout();

            var previous = CurrentUser;

            CurrentUser = _anonymousUser;
        }

        public bool IsAuthenticated
        {
            get
            {
                return (CurrentUser != _anonymousUser);
            }
        }

        public bool IsConnected { get; private set; }

        public IMediaBrowserService Service
        {
            get
            {
                if (_service != null || !GlobalServiceProvider.IsRegistered<IMediaBrowserService>()) return _service;
                _service = GlobalServiceProvider.Get<IMediaBrowserService>();
                return _service;
            }
        } IMediaBrowserService _service;

        public void Update(BaseItemDto item, string context = "")
        {
            Update(item.Type, item.Id, item.Name, context);
        }

        public async void Update(string itemType, string itemId, string itemName, string context = "")
        {
            if (Client != null )
            {
                if (itemType == "View")
                {
                    itemType = "";
                    itemId = "";
                }

                try
                {
                    await Client.SendContextMessageAsync(itemType, itemId, itemName, context, CancellationToken.None);
                }
                catch (Exception e)
                {
                    MediaBrowserPlugin.Log.Error(e);
                }
                
            }
        }

        public async void PublishUser()
        {
            await Task.Run(() => GUISession.Instance.CurrentUser.Publish(MediaBrowserPlugin.DefaultProperty + ".User"));
        }

        private async void OnRemoteLoggedOut(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                await Logout();
            }
        }

        private void OnLibraryChanged(object sender, GenericEventArgs<LibraryUpdateInfo> e)
        {
            _logger.Debug("OnLibraryChanged");
            
            // todo: update items in memory
            var info = e.Argument;
        }

        private void OnSocketMessageCommand(object sender, GenericEventArgs<MessageCommand> e)
        {
            _logger.Debug("Message: {0}", e.Argument.Text);
        }

        private void OnPlayCommand(object sender, GenericEventArgs<PlayRequest> args)
        {
            var request = args.Argument;
            // todo: support multiple ids
            _logger.Info("Remote Play Request: Id={1}, StartPositionTicks={2}", request.ItemIds[0],
                args.Argument.StartPositionTicks);
            var resumeTime = (int)TimeSpan.FromTicks(request.StartPositionTicks ?? 0).TotalSeconds;

            GUICommon.Window(MediaBrowserWindow.Details, MediaBrowserMedia.Play(request.ItemIds[0], resumeTime));
        }

        private void OnBrowseCommand(object sender, GenericEventArgs<BrowseRequest> args)
        {
            var request = args.Argument;
            _logger.Info("Remote Browse Request: Type={0}, Id={1}, Name={2}", request.ItemType, request.ItemId,
                args.Argument.ItemName);

            switch (request.ItemType)
            {
                case "Movie":
                    GUICommon.Window(MediaBrowserWindow.Details, MediaBrowserMedia.Browse(request.ItemId));
                    return;
                default:
                    GUICommon.Window(MediaBrowserWindow.Main,
                        new MediaBrowserItem
                        {
                            Id = request.ItemId,
                            Type = request.ItemType,
                            Name = request.ItemName
                        });
                    return;
            }
        }
    }
}
