using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using MediaPortal.Configuration;
using MediaPortal.GUI.Library;

namespace Pondman.MediaPortal
{
    public class SettingsManager<TResource> where TResource : class, new()
    {
        readonly ILogger _logger;
        readonly string _path = string.Empty;

        public SettingsManager(string name, ILogger logger = null)
        {
            _logger = logger ?? NullLogger.Instance;
            _path = Config.GetFile(Config.Dir.Config, name + ".xml");
            Load();
        }

        /// <summary>
        /// Gets the settings.
        /// </summary>
        /// <value>
        /// The settings.
        /// </value>
        public TResource Settings { get; private set; }

        /// <summary>
        /// Gets the path to the settings file.
        /// </summary>
        /// <value>
        /// The settings file.
        /// </value>
        public string Path
        {
            get
            {
                return _path;
            }
        }

        /// <summary>
        /// Loads setings from file
        /// </summary>
        public void Load()
        {
            if (File.Exists(_path))
            {
                try
                {
                    var ser = new DataContractSerializer(typeof (TResource));
                    using (var fs = new FileStream(_path, FileMode.Open))
                    using (var reader = XmlDictionaryReader.CreateTextReader(fs, new XmlDictionaryReaderQuotas()))
                    {
                        Settings = (TResource) ser.ReadObject(reader, true);
                        reader.Close();
                        fs.Close();
                    }
                    return;
                }
                catch (Exception e)
                {
                    _logger.Error("Cannot read existing settings file.", e.Message);
                }
            }

            _logger.Info("Creating new settings file.");
            Settings = new TResource();
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
                if (File.Exists(_path))
                {
                    try
                    {
                        File.Copy(_path, _path.Replace(".xml", ".bak"), true);
                    }
                    catch (Exception e)
                    {
                        _logger.Error("Cannot create backup file for settings.", e);
                    }
                }

                try
                {
                    var ser = new DataContractSerializer(typeof (TResource));
                    using (var writer = new FileStream(_path, FileMode.Create))
                    {
                        ser.WriteObject(writer, Settings);
                        writer.Close();
                    }
                }
                catch (Exception e)
                {
                    _logger.Error("Cannot create settings file.", e);
                }
            }
        }

    }
}
