using System;
using System.Net;
using System.Reflection;
using System.Threading;
using MediaBrowser.ApiInteraction;
using MediaBrowser.ApiInteraction.WebSocket;
using MediaBrowser.Model.System;
using Pondman.MediaPortal.MediaBrowser.GUI;
using Pondman.MediaPortal.MediaBrowser.Models;

namespace Pondman.MediaPortal.MediaBrowser
{
    public class MediaBrowserService : IMediaBrowserService // todo: disposable?
    {
        const string MediaBrowserModelAssemblyVersion = "3.0.5021.27473";
        const string MediaBrowserModelAssembly = "MediaBrowser.Model, Version=" + MediaBrowserModelAssemblyVersion + ", Culture=neutral, PublicKeyToken=6cde51960597a7f9";
        
        #region Private variables

        private readonly ServerLocator _locator;
        private readonly ILogger _logger;
        private readonly MediaBrowserPlugin _plugin;

        Timer _retryTimer;

        #endregion

        public MediaBrowserService(MediaBrowserPlugin plugin, ILogger logger = null)
        {
            // Assembly rebinding for Media Browser
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            
            _locator = new ServerLocator();
            _logger = logger ?? NullLogger.Instance;
            _plugin = plugin;
            _logger.Info("MediaBrowserService initialized.");
        }

        public event Action<IPEndPoint> ServerChanged;

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
        public IPEndPoint Server
        {
            get { return _endpoint; }
            set
            {
                _endpoint = value;
                OnServerChanged(_endpoint);
            }
        } IPEndPoint _endpoint;

        public bool IsServerLocated
        {
            get { return (Server != null); }
        }

        public SystemInfo System { get; internal set; }

        public MediaBrowserClient Client
        {
            get { return _client; }
            internal set
            {
                _client = value;
                Update();
            }
        } MediaBrowserClient _client;

        public void Update()
        {
            if (!IsServerLocated) return;
            Client.GetSystemInfo(info =>
            {
                System = info;
                if (Client.WebSocketConnection == null)
                {
                    StartWebSocket();
                }
            }, _logger.Error);
        }

        public virtual void Discover()
        {
            _retryTimer = new Timer(x =>
            {
                _logger.Info("Discovering Media Browser Server.");
                _locator.FindServer(OnServerDiscovered);
            }, null, 0, 60000);
        }

        // todo: create a WebSocket listener class that you can attach to a client
        // todo: move command handlers to GUI code

        protected virtual void StartWebSocket()
        {
            _logger.Info("Connecting to Media Browser Server.");
            var socket = Client.WebSocketConnection = new ApiWebSocket(
                Client.ServerHostName, 
                System.WebSocketPortNumber, 
                Client.DeviceId, 
                Client.ApplicationVersion, 
                Client.ClientName, 
                new WebSocket4NetClientWebSocket()
            );

            socket.PlayCommand += OnPlayCommand;
            socket.BrowseCommand += OnBrowseCommand;
            socket.Connect(RetryWebSocket);
        }

        private void RetryWebSocket(Exception e)
        {
            _logger.Error(e);
            _logger.Info("Lost connection with Media Browser Server.");
            _retryTimer = new Timer(x =>
            {
                _logger.Info("Reconnecting to Media Browser Server.");
                Client.WebSocketConnection.Connect(RetryWebSocket);
            }, null, 15000, Timeout.Infinite);
        }

        protected void OnServerDiscovered(IPEndPoint endpoint)
        {
            if (_retryTimer == null) return;

            _retryTimer.Dispose();
            _retryTimer = null;

            _logger.Info("Found MediaBrowser Server: {0}", endpoint);
            Server = endpoint;
        }

        protected void OnServerChanged(IPEndPoint endpoint)
        {
            _logger.Debug("Creating Media Browser client.");
            var client = new MediaBrowserClient(
                            endpoint.Address.ToString(), 
                            endpoint.Port,
                            Environment.OSVersion.VersionString, 
                            Environment.MachineName, 
                            Plugin.Version.ToString()
                            );
            Client = client;

            if (ServerChanged != null)
            {
                ServerChanged(endpoint);
            }
        }

        protected void OnPlayCommand(object sender, PlayRequestEventArgs args)
        {
            // todo: support multiple ids
            _logger.Info("Remote Play Request: Id={1}, StartPositionTicks={2}", args.Request.ItemIds[0],
                args.Request.StartPositionTicks);
            var resumeTime = (int)TimeSpan.FromTicks(args.Request.StartPositionTicks ?? 0).TotalSeconds;

            GUICommon.Window(MediaBrowserWindow.Movie, MediaBrowserMedia.Play(args.Request.ItemIds[0], resumeTime));
        }

        protected void OnBrowseCommand(object sender, BrowseRequestEventArgs args)
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

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // this is a work around to load the expected assembly
            var requestedName = new AssemblyName(args.Name);
            if (requestedName.Name == "MediaBrowser.Model" && requestedName.Version.ToString() != MediaBrowserModelAssemblyVersion)
            {
                return Assembly.Load(MediaBrowserModelAssembly);
            }
            else
            {
                return null;
            }
        }
    }
}