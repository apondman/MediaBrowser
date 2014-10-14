using MediaPortal.GUI.Library;
using MediaPortal.Profile;
using System;
using System.Reflection;

namespace Pondman.MediaPortal
{
    public interface ISettings
    {
        void OnLoad();

        void OnSave();
    }

    public class SettingsManager<TResource> where TResource : class, ISettings, new()
    {
        readonly ILogger _logger;
        readonly string _name;
        readonly PropertyInfo[] _properties;

        public SettingsManager(string name, ILogger logger = null)
        {
            _logger = logger ?? NullLogger.Instance;
            _name = name;
            _properties = typeof(TResource).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            GUIWindowManager.OnDeActivateWindow += GUIWindowManager_OnDeActivateWindow;
            Load();
        }

        void GUIWindowManager_OnDeActivateWindow(int windowId)
        {
            if (windowId == 803)
            {
                _logger.Debug("Reloading settings.");
                Load();
            }
        }

        /// <summary>
        /// Gets the settings.
        /// </summary>
        /// <value>
        /// The settings.
        /// </value>
        public TResource Settings { get; private set; }

        /// <summary>
        /// Loads setings from file
        /// </summary>
        public void Load()
        {
            var settings = new TResource();
            using (var mp = new MPSettings())
            {
                foreach (var p in _properties)
                {
                    try
                    {
                        var value = mp.GetValue(_name, p.Name);
                        if (string.IsNullOrEmpty(value)) continue;

                        var safeType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;

                        object safeValue = (value == null) ? null : Convert.ChangeType(value, safeType);

                        p.SetValue(settings, safeValue);
                    }
                    catch (Exception e)
                    {
                        _logger.Error("Cannot load setting '" + p.Name + "'", e.Message);
                    }
                }
            }

            settings.OnLoad();
            Settings = settings;

            Save();
        }

        /// <summary>
        /// Saves settings to file
        /// </summary>
        public void Save()
        {
            if (Settings == null) return;

            lock (Settings)
            {
                _logger.Info("Saving settings and cache...");
                
                using (var mp = new MPSettings())
                {
                    foreach (var p in _properties)
                    {
                        try
                        {
                            mp.SetValue(_name, p.Name, p.GetValue(Settings));
                        }
                        catch (Exception e)
                        {
                            _logger.Error("Cannot save setting '" + p.Name + "'", e.Message);
                        }
                    }
                }

                // trigger additional logic
                Settings.OnSave();

                // trigger mediaportal settings cache
                global::MediaPortal.Profile.Settings.SaveCache();

                _logger.Info("Settings saved.");
              
            }
        }

    }
}
