using MediaPortal.Configuration;
using MediaPortal.GUI.Library;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Pondman.MediaPortal
{
    public class i18n<TResource> where TResource : class, new()
    {
        #region Private variables

        TResource _resource;
        Dictionary<string, string> _translations;
        List<FieldInfo> _strings;
        ILogger _logger;
        Regex translateExpr = new Regex(@"\$\{([^\}]+)\}");
        string _path = string.Empty;

        #endregion

        #region Constructor

        public i18n(string folder, ILogger logger = null)
        {
            _logger = logger ?? NullLogger.Instance;
            _translations = new Dictionary<string, string>();
            _strings = typeof(TResource).GetFields().ToList();

            string lang;
            try
            {
                lang = GUILocalizeStrings.GetCultureName(GUILocalizeStrings.CurrentLanguage());
            }
            catch (Exception)
            {
                lang = CultureInfo.CurrentUICulture.Name;
            }

            _logger.Info("Using language " + lang);

            _path = Config.GetSubFolder(Config.Dir.Language, folder);

            if (!System.IO.Directory.Exists(_path))
                System.IO.Directory.CreateDirectory(_path);

            LoadTranslations(lang);
        }

        #endregion

        #region Public Properties

        public TResource Resource
        {
            get
            {
                return _resource;
            }
        }

        /// <summary>
        /// Gets the translated strings collection in the active language
        /// </summary>
        public Dictionary<string, string> Strings
        {
            get
            {
                return _translations;
            }
        }

        #endregion

        #region Public Methods

        public int LoadTranslations(string lang)
        {
            XDocument resource;
            string langPath = string.Empty;
            Dictionary<string, string> localTranslations;

            try
            {
                langPath = Path.Combine(_path, lang + ".xml");
                resource = XDocument.Load(langPath);
            }
            catch (Exception e)
            {
                if (lang == "en")
                    return 0; // otherwise we are in an endless loop!

                if (e.GetType() == typeof(FileNotFoundException))
                    _logger.Warn("Cannot find translation file {0}. Falling back to English", langPath);
                else
                    _logger.Error("Error in translation xml file: {0}. Falling back to English", lang);

                return LoadTranslations("en");
            }

            try
            {
                localTranslations = resource
                    .Descendants("string")
                    .ToDictionary<XElement, string, string>(x => x.Attribute("name").Value, x => Regex.Unescape(x.Value));
            }
            catch (Exception ex)
            {
                _logger.Error("Error in Translation Engine: {0}", ex.Message);
                return 0;
            }

            // update strings in resource
            int translated = localTranslations.Count();
            
            // instantiate a new language resource
            _resource = new TResource();

            string value;
            foreach(var field in _strings) 
            {
                if (localTranslations.TryGetValue(field.Name, out value))
                {
                    // update the language resource value
                    field.SetValue(_resource, value);
                }
                else
                {
                    // get the default value from the resource
                    value = (string)field.GetValue(_resource);
                    // log that we are missing a translation key
                    _logger.Info("Missing string: Key={0}, Default={1}", field.Name, value);
                }

                // update the value in the translation dictionary
                _translations[field.Name] = value;
            }

            // return the amount of translations in the loaded file.
            return localTranslations.Count;
        }

        public string GetByName(string name)
        {
            string value = null;
            if (_translations.TryGetValue(name, out value))
            {
                return value;
            }

            return name;
        }

        public string GetByName(string name, params object[] args)
        {
            string translation = GetByName(name);

            try
            {
                return translation.Format(args);
            }
            catch (Exception)
            {
                return translation;
            }
        }

        /// <summary>
        /// Takes an input string and replaces all ${named} variables with the proper translation if available
        /// </summary>
        /// <param name="input">a string containing ${named} variables that represent the translation keys</param>
        /// <returns>translated input string</returns>
        public string ParseString(string input)
        {
            MatchCollection matches = translateExpr.Matches(input);
            foreach (Match match in matches)
            {
                input = input.Replace(match.Value, GetByName(match.Groups[1].Value));
            }
            return input;
        }

        public void Publish(string tag = "#", params string[] exclude)
        {
            // make sure the keys do not overlap by the skin engine
            var labels = _translations.ToDictionary(x => x.Key + ".Label", x => x.Value);

            GUIUtils.Publish(labels, tag, exclude);
        }

        #endregion

    }

}
