using System;
using System.Net;
using System.Reflection;
using System.Threading;
using MediaBrowser.ApiInteraction;
using MediaBrowser.ApiInteraction.WebSocket;
using MediaBrowser.Model.ApiClient;
using MediaBrowser.Model.System;
using MediaPortal.ExtensionMethods;
using Pondman.MediaPortal.MediaBrowser.GUI;
using Pondman.MediaPortal.MediaBrowser.Models;

namespace Pondman.MediaPortal.MediaBrowser
{
    public class MediaBrowserService : IMediaBrowserService, IDisposable
    {
        const string MediaBrowserModelAssemblyVersion = "3.0.5021.27473";
        const string MediaBrowserModelAssembly = "MediaBrowser.Model, Version=" + MediaBrowserModelAssemblyVersion + ", Culture=neutral, PublicKeyToken=6cde51960597a7f9";
        
        #region Private variables

        private readonly ServerLocator _locator;
        private readonly ILogger _logger;
        private readonly MediaBrowserPlugin _plugin;
        private bool _disposed;

        Timer _retryTimer;

        #endregion

        public MediaBrowserService(MediaBrowserPlugin plugin, ILogger logger = null)
        {
            // Assembly rebinding for Media Browser
            // AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            
            _locator = new ServerLocator();
            _logger = logger ?? NullLogger.Instance;
            _plugin = plugin;
            _logger.Info("MediaBrowserService initialized.");
        }

        public event Action<IPEndPoint> ServerChanged;

        public event Action<SystemInfo> SystemInfoChanged;

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

        public SystemInfo System
        {
            get { return _systemInfo; }
            internal set
            {
                _systemInfo = value;
                if (SystemInfoChanged != null)
                {
                    SystemInfoChanged(value);
                }
            } 
        } SystemInfo _systemInfo;

        public MediaBrowserClient Client
        {
            get { return _client; }
            internal set
            {
                _client = value;
                Update();
            }
        } MediaBrowserClient _client;

        public virtual void Discover()
        {
            _retryTimer = new Timer(x =>
            {
                _logger.Info("Discovering Media Browser Server.");
                _locator.FindServer(OnServerDiscovered);
            }, null, 0, 60000);
        }

        public void Update()
        {
            if (!IsServerLocated) return;
            Client.GetSystemInfo(info => System = info, MediaBrowserPlugin.Log.Error);
            ApiWebSocket.Create(Client, OnConnecting, _logger.Error);
        }

        protected void OnConnecting(ApiWebSocket socket)
        {
            socket.MessageCommand += OnSocketMessageCommand;
            socket.PlayCommand += OnPlayCommand;
            socket.BrowseCommand += OnBrowseCommand;
            socket.Connected += OnSocketConnected;
            socket.Disconnected += OnSocketDisconnected;
            
            _logger.Info("Connecting to Media Browser Server.");
            socket.Connect(true);
        }

        void OnSocketConnected(object sender, EventArgs e)
        {
            _logger.Info("Connected to Media Browser Server.");
        }

        void OnSocketDisconnected(object sender, EventArgs e)
        {
            _logger.Info("Lost connection with Media Browser Server.");
        }

        void OnSocketMessageCommand(object sender, MessageCommandEventArgs e)
        {
            _logger.Debug("Message: {0}", e.Request.Text);
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

        // todo: move command handlers to GUI code

        protected void OnPlayCommand(object sender, PlayRequestEventArgs args)
        {
            // todo: support multiple ids
            _logger.Info("Remote Play Request: Id={1}, StartPositionTicks={2}", args.Request.ItemIds[0],
                args.Request.StartPositionTicks);
            var resumeTime = (int)TimeSpan.FromTicks(args.Request.StartPositionTicks ?? 0).TotalSeconds;

            GUICommon.Window(MediaBrowserWindow.Details, MediaBrowserMedia.Play(args.Request.ItemIds[0], resumeTime));
        }

        protected void OnBrowseCommand(object sender, BrowseRequestEventArgs args)
        {
            _logger.Info("Remote Browse Request: Type={0}, Id={1}, Name={2}", args.Request.ItemType, args.Request.ItemId,
                args.Request.ItemName);

            switch (args.Request.ItemType)
            {
                case "Movie":
                    GUICommon.Window(MediaBrowserWindow.Details, MediaBrowserMedia.Browse(args.Request.ItemId));
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
                if (_retryTimer != null)
                    _retryTimer.Dispose();
                
                if (Client != null && Client.WebSocketConnection != null)
                    Client.WebSocketConnection.Dispose();

                _logger.Info("MediaBrowserService shutdown.");
            }

            _disposed = true;
        }

    }
}