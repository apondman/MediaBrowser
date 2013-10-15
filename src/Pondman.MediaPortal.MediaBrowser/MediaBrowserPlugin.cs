using MediaBrowser.Model.System;
using MediaPortal.Configuration;
using MediaPortal.GUI.Library;
using MediaPortal.Services;
using Pondman.MediaPortal.MediaBrowser.GUI;
using Pondman.MediaPortal.MediaBrowser.Resources.Languages;
using System;

namespace Pondman.MediaPortal.MediaBrowser
{
    /// <summary>
    /// The MediaBrowser Plugin for MediaPortal
    /// </summary>
    [PluginIcons("Pondman.MediaPortal.MediaBrowser.Resources.Images.mblogoicon.png", "Pondman.MediaPortal.MediaBrowser.Resources.Images.mblogoicon.png")]
    public class MediaBrowserPlugin : PluginBase
    {
        private static readonly Action<string, string> LogShit = (tag, tagValue) =>
        {
            if (!Config.Settings.LogProperties || !tag.StartsWith(DefaultProperty))
                return;

            if (tagValue != " ")
            {
                Log.Debug("SET: \"" + tag + ": \"" + tagValue + "\"");
            }
            else
            {
                Log.Debug("UNSET: \"" + tag + "\"");
            }
        };

        
        #region Ctor

        public MediaBrowserPlugin()
            : base((int)MediaBrowserWindow.Main)
        {           
            // check if we have registered the MediaBrowserService
            bool isServiceRegistered = GlobalServiceProvider.IsRegistered<IMediaBrowserService>();
            if (!isServiceRegistered)
            {
                Log.Debug("Registering MediaBrowserService.");

                // Publish Default System Info
                GUIContext.OnSystemInfoChanged(new SystemInfo());

                // Publish Default User
                GUIContext.Instance.PublishUser();

                // if not register it with the global service provider
                IMediaBrowserService service = new MediaBrowserService(this, MediaBrowserPlugin.Log);

                // add event handlers
                service.SystemInfoChanged += GUIContext.OnSystemInfoChanged;

                // add service to the global service provider
                GlobalServiceProvider.Add(service);

                // trigger discovery so client gets loaded.
                service.Discover();
            }

            // setup property management
            GUIPropertyManager.OnPropertyChanged += GUIPropertyManager_OnPropertyChanged;
            
            // version information
            Version.Publish(MediaBrowserPlugin.DefaultProperty + ".Version");
            VersionName.Publish(MediaBrowserPlugin.DefaultProperty + ".Version.Name");
            GUIUtils.Publish(MediaBrowserPlugin.DefaultProperty + ".Version.String", Version.ToString());

            // Translations
            UI.Publish(DefaultProperty + ".Translation");

            // Settings
            Config.Settings.Publish(DefaultProperty + ".Settings");        

        }

        ~MediaBrowserPlugin() 
        {
            GUIPropertyManager.OnPropertyChanged -= GUIPropertyManager_OnPropertyChanged;
        }

        #endregion

        #region Handlers

        static void GUIPropertyManager_OnPropertyChanged(string tag, string tagValue)
        {
            LogShit.BeginInvoke(tag, tagValue, LogShit.EndInvoke, null);
        }

        #endregion

        #region Overrides

        public override bool HasSetup()
        {
            return false;
        }

        public override bool GetHome(out string strButtonText, out string strButtonImage, out string strButtonImageFocus,
            out string strPictureImage)
        {
            strButtonText = PluginName();
            strButtonImage = string.Empty;
            strButtonImageFocus = string.Empty;
            strPictureImage = @"hover_MediaBrowser.png";

            return true;
        }

        #endregion        

        #region Core 

        public static readonly string DefaultProperty = "#MediaBrowser";

        public static readonly string DefaultName = "MediaBrowser";

        /// <summary>
        /// Wrapper for log4net
        /// </summary>
        public static readonly ILogger Log = new Log4NetLogger(MediaBrowserPlugin.DefaultName);

        /// <summary>
        /// Translations
        /// </summary>
        public static readonly i18n<Translations> UI = new i18n<Translations>(MediaBrowserPlugin.DefaultName, Log);

        /// <summary>
        /// Settings
        /// </summary>
        public static readonly SettingsManager<MediaBrowserSettings> Config = new SettingsManager<MediaBrowserSettings>(MediaBrowserPlugin.DefaultName, Log);

        #endregion

    }
}
