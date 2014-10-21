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
using MediaBrowser.ApiInteraction.Network;
using System.Security.Principal;
using System.DirectoryServices;
using System.Linq;
using System.Collections.Generic;
using MediaBrowser.ApiInteraction.Cryptography;

namespace Pondman.MediaPortal.MediaBrowser
{
    public class MediaBrowserService : IMediaBrowserService
    {

        const string CLIENT_NAME = "MediaPortal";

        #region Private variables

        private readonly ILogger _logger;
        private readonly MediaBrowserPlugin _plugin;
        private bool _disposed;
        IConnectionManager _connectionManager;

        #endregion

        public MediaBrowserService(MediaBrowserPlugin plugin, ILogger logger = null)
        {
            _logger = logger ?? NullLogger.Instance;
            _plugin = plugin;
            
            _logger.Info("MediaBrowserService initialized.");
        }

        public IConnectionManager ConnectionManager
        {
            get
            {
                if (_connectionManager != null) return _connectionManager;

                var logger = new MediaBrowserLogger(_logger);

                var device = new Device
                {
                    DeviceName = Environment.MachineName,
                    DeviceId = GetComputerSid().ToString()
                };

                var capabilities = new ClientCapabilities
                {
                    PlayableMediaTypes = new List<string>
                    {
                        MediaType.Audio,
                        MediaType.Video
                        //MediaType.Game,
                        //MediaType.Photo,
                        //MediaType.Book
                    },

                    SupportedCommands = new List<String>(Enum.GetNames(typeof(GeneralCommandType)))
                };

                var credentialProvider = new CredentialProvider();
                var networkConnection = new NetworkConnection(logger);
                var serverLocator = new ServerLocator(logger);
                var cryptoProvider = new CryptographyProvider();

                var connectionManager = new ConnectionManager(logger,
                    credentialProvider,
                    networkConnection,
                    serverLocator,
                    CLIENT_NAME,
                    Plugin.Version.ToString(),
                    device,
                    capabilities,
                    cryptoProvider,
                    ClientWebSocketFactory.CreateWebSocket);

                _connectionManager = connectionManager;

                return _connectionManager;
            }
        }

        public MediaBrowserPlugin Plugin
        {
            get { return _plugin; }
        }

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
                if (ConnectionManager != null)
                {
                    ConnectionManager.Logout();
                }

                _logger.Info("MediaBrowserService shutdown.");
            }

            _disposed = true;
        }

        #endregion

        public static SecurityIdentifier GetComputerSid()
        {
            return new SecurityIdentifier((byte[])new DirectoryEntry(string.Format("WinNT://{0},Computer", Environment.MachineName)).Children.Cast<DirectoryEntry>().First().InvokeGet("objectSID"), 0).AccountDomainSid;
        }
    }
}