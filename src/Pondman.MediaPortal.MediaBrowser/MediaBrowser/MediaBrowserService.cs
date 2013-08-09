using System;
using System.Net;
using MediaBrowser.ApiInteraction;
using MediaBrowser.ApiInteraction.WebSocket;
using MediaBrowser.Model.System;
using Pondman.MediaPortal.MediaBrowser.GUI;
using Pondman.MediaPortal.MediaBrowser.Models;

namespace Pondman.MediaPortal.MediaBrowser
{
    public class MediaBrowserService : IMediaBrowserService // todo: disposable?
    {
        #region Private variables

        private readonly ILogger _logger;
        private readonly MediaBrowserPlugin _plugin;
        private IPEndPoint _endpoint;

        #endregion

        public MediaBrowserService(MediaBrowserPlugin plugin, ILogger logger = null)
        {
            _logger = logger ?? NullLogger.Instance;
            _plugin = plugin;
            _logger.Info("MediaBrowserService initialized.");
        }

        public event Action<IPEndPoint> ServerChanged;

        public virtual MediaBrowserPlugin Plugin
        {
            get { return _plugin; }
        }

        /// <summary>
        ///     Gets or sets the endpoint of the server.
        /// </summary>
        /// <value>
        ///     The server endpoint.
        /// </value>
        public virtual IPEndPoint Server
        {
            get { return _endpoint; }
            set
            {
                _endpoint = value;
                OnServerChanged(_endpoint);
            }
        }

        public virtual bool IsServerLocated
        {
            get { return (Server != null); }
        }

        public virtual SystemInfo System { get; internal set; }

        public virtual MediaBrowserClient Client { get; internal set; }

        public virtual void Discover()
        {
            var locator = new ServerLocator();
            locator.FindServer(OnServerDiscovered);
        }

        public virtual void Update()
        {
            if (IsServerLocated)
            {
                Client.GetSystemInfo(info =>
                {
                    System = info;
                    if (Client.WebSocketConnection == null)
                    {
                        StartWebSocket();
                    }
                }, _logger.Error);
            }
            else
            {
                Discover();
            }
        }

        // todo: create a WebSocket listener class that you can attach to a client
        // todo: move command handlers to GUI code

        protected virtual void StartWebSocket()
        {
            MediaBrowserClient client = Client;
            ApiWebSocket socket = client.WebSocketConnection = new ApiWebSocket(new WebSocket4NetClientWebSocket());
            socket.PlayCommand += OnPlayCommand;
            socket.BrowseCommand += OnBrowseCommand;
            socket.Connect(client.ServerHostName, System.WebSocketPortNumber, client.ClientName, client.DeviceId,
                client.ApplicationVersion, _logger.Error);
        }

        private void OnPlayCommand(object sender, PlayRequestEventArgs args)
        {
            // todo: support multiple ids
            _logger.Info("Remote Play Request: Id={1}, StartPositionTicks={2}", args.Request.ItemIds[0],
                args.Request.StartPositionTicks);
            var resumeTime = (int) TimeSpan.FromTicks(args.Request.StartPositionTicks ?? 0).TotalSeconds;

            GUICommon.Window(MediaBrowserWindow.Movie, MediaBrowserMedia.Play(args.Request.ItemIds[0], resumeTime));
        }

        private void OnBrowseCommand(object sender, BrowseRequestEventArgs args)
        {
            _logger.Info("Remote Browse Request: Type={0}, Id={1}, Name={2}", args.Request.ItemType, args.Request.ItemId,
                args.Request.ItemName);

            switch (args.Request.ItemType)
            {
                case "Movie":
                    GUICommon.Window(MediaBrowserWindow.Movie, MediaBrowserMedia.Browse(args.Request.ItemId));
                    return;
                default:
                    GUICommon.Window(MediaBrowserWindow.Main,
                        new MediaBrowserItem
                        {
                            Id = args.Request.ItemId,
                            Type = args.Request.ItemType,
                            Name = args.Request.ItemName
                        });
                    return;
            }
        }

        protected virtual void OnServerDiscovered(IPEndPoint endpoint)
        {
            _logger.Info("Found MediaBrowser Server: {0}", endpoint);
            Server = endpoint;
        }

        protected virtual void OnServerChanged(IPEndPoint endpoint)
        {
            _logger.Debug("Creating Default Media Browser API Client.");
            var client = new MediaBrowserClient(endpoint.Address.ToString(), endpoint.Port,
                Environment.OSVersion.VersionString, Environment.MachineName, Plugin.Version.ToString());
            Client = client;
            Update();

            if (ServerChanged != null)
            {
                ServerChanged(endpoint);
            }
        }
    }
}