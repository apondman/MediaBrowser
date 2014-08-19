using System;
using System.Net;
using System.Reflection;
using System.Threading;
using MediaBrowser.ApiInteraction;
using MediaBrowser.ApiInteraction.WebSocket;
using MediaBrowser.Model.ApiClient;
using MediaBrowser.Model.System;
using MediaPortal.ExtensionMethods;
using Pondman.MediaPortal.MediaBrowser.Events;
using Pondman.MediaPortal.MediaBrowser.GUI;
using Pondman.MediaPortal.MediaBrowser.Models;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.Entities;

namespace Pondman.MediaPortal.MediaBrowser
{
    public class MediaBrowserService : IMediaBrowserService
    {
        
        #region Private variables

        private readonly ILogger _logger;
        private readonly MediaBrowserPlugin _plugin;
        private bool _disposed;

        #endregion

        public MediaBrowserService(MediaBrowserPlugin plugin, ILogger logger = null)
        {
            _logger = logger ?? NullLogger.Instance;
            _plugin = plugin;
            _logger.Info("MediaBrowserService initialized.");
        }

        #region Events

        public event EventHandler<SystemInfoChangedEventArgs> SystemInfoChanged;

        #endregion

        public MediaBrowserPlugin Plugin
        {
            get { return _plugin; }
        }

        /// <summary>
        ///     Gets or sets the endpoint of the server.
        /// </summary>
        /// <value>
        ///     The server endpoint.
        /// </value>
        public ServerDiscoveryInfo Server
        {
            get { return _serverInfo; }
            set
            {
                _serverInfo = value;
                OnServerChanged(_serverInfo);
            }
        } ServerDiscoveryInfo _serverInfo;

        public bool IsServerLocated
        {
            get { return (Server != null); }
        }

        public PublicSystemInfo System
        {
            get { return _systemInfo; }
            internal set
            {
                _systemInfo = value;
                SystemInfoChanged.FireEvent(this, new SystemInfoChangedEventArgs(_systemInfo));
            } 
        } PublicSystemInfo _systemInfo;

        public MediaBrowserClient Client
        {
            get { return _client; }
            internal set
            {
                _client = value;
                StartWebSocket();
            }
        } MediaBrowserClient _client;

        public async void Discover(int retryIntervalMs = 60000)
        {
            _logger.Info("Discovering Media Browser Server.");
            while (true)
            {
                try
                {
                    var servers = await new ServerLocator().FindServers(CancellationToken.None);
                    foreach (var server in servers)
                    {
                        // todo: allow server selection
                        OnServerDiscovered(server);
                    }

                    return;
                }
                catch (Exception e)
                {
                    _logger.Error(e);
                }
            }
        }

        public async void StartWebSocket() 
        {
            _logger.Info("Connecting to Media Browser Server.");
            var socket = await ApiWebSocket.Create(_client, ClientWebSocketFactory.CreateWebSocket, CancellationToken.None);
            _logger.Info("Connected to Media Browser Server.");

            socket.MessageCommand += OnSocketMessageCommand;
            socket.PlayCommand += OnPlayCommand;
            socket.BrowseCommand += OnBrowseCommand;
            socket.LibraryChanged += OnLibraryChanged;  
            socket.Closed += OnSocketDisconnected;

            await socket.EnsureConnectionAsync(CancellationToken.None);
            _client.WebSocketConnection.StartEnsureConnectionTimer(10000);
            Update();
        }

        void OnLibraryChanged(object sender, GenericEventArgs<LibraryUpdateInfo> e)
        {
            // todo: update items in memory
            var info = e.Argument;
        }

        public async void Update()
        {
            if (!IsServerLocated) return;

            try
            {
                System = await Client.GetPublicSystemInfoAsync(CancellationToken.None);
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }

        #region internal event handlers


        void OnSocketDisconnected(object sender, EventArgs e)
        {
            _logger.Info("Lost connection with Media Browser Server.");
        }

        void OnSocketMessageCommand(object sender, GenericEventArgs<MessageCommand> e)
        {
            _logger.Debug("Message: {0}", e.Argument.Text);
        }

        protected void OnServerDiscovered(ServerDiscoveryInfo info)
        {
            _logger.Info("Found MediaBrowser Server: {0}", info.Address);
            Server = info;
        }

        protected void OnServerChanged(ServerDiscoveryInfo info)
        {
            _logger.Debug("Creating Media Browser client.");
            var client = new MediaBrowserClient(
                            info.Address,
                            Environment.MachineName + " (" + Environment.OSVersion.VersionString + ")", // todo: add MediaPortal version instead of OS
                            Environment.MachineName,
                            Plugin.Version.ToString()
                            );
            Client = client;
            //ServerChanged.FireEvent(this, new ServerChangedEventArgs(endpoint));
        }

        // todo: move command handlers to GUI code

        protected void OnPlayCommand(object sender, GenericEventArgs<PlayRequest> args)
        {
            var request = args.Argument;
            // todo: support multiple ids
            _logger.Info("Remote Play Request: Id={1}, StartPositionTicks={2}", request.ItemIds[0],
                args.Argument.StartPositionTicks);
            var resumeTime = (int)TimeSpan.FromTicks(request.StartPositionTicks ?? 0).TotalSeconds;

            GUICommon.Window(MediaBrowserWindow.Details, MediaBrowserMedia.Play(request.ItemIds[0], resumeTime));
        }

        protected void OnBrowseCommand(object sender, GenericEventArgs<BrowseRequest> args)
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

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                if (Client != null && Client.WebSocketConnection != null)
                {
                    Client.WebSocketConnection.StopEnsureConnectionTimer();
                    Client.WebSocketConnection.StopReceivingSessionUpdates();
                    Client.WebSocketConnection.Dispose();
                }

                _logger.Info("MediaBrowserService shutdown.");
            }

            _disposed = true;
        }

        #endregion

    }
}