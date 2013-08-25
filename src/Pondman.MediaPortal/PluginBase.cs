using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using MediaPortal.Configuration;
using MediaPortal.GUI.Library;

namespace Pondman.MediaPortal
{
    [PluginIcons("Pondman.MediaPortal.Resources.Images.plugin.png", "Pondman.MediaPortal.Resources.Images.plugin.png")]
    public abstract class PluginBase : IPlugin, ISetupForm
    {
        readonly string _name;
        readonly string _author;
        readonly string _description;
        readonly int _pluginId;
        readonly Version _version;
        readonly string _versionName;

        protected PluginBase(int id)
        {
            Assembly asm = Assembly.GetCallingAssembly();
            foreach (Attribute attr in Attribute.GetCustomAttributes(asm))
            {
                if (attr is AssemblyTitleAttribute)
                {
                    this._name = ((AssemblyTitleAttribute)attr).Title;
                }
                else if (attr is AssemblyDescriptionAttribute)
                {
                    this._description = ((AssemblyDescriptionAttribute)attr).Description;
                }
                else if (attr is AssemblyCompanyAttribute)
                {
                    this._author = ((AssemblyCompanyAttribute)attr).Company;
                }
                else if (attr is AssemblyInformationalVersionAttribute)
                {
                    this._versionName = ((AssemblyInformationalVersionAttribute)attr).InformationalVersion;
                }
            }

            _version = asm.GetName().Version;
            _pluginId = id;
        }

        public string VersionName
        {
            get
            {
                return _versionName;
            }
        }

        public Version Version
        {
            get
            {
                return _version;
            }
        }

        #region ISetupForm

        public virtual string Author()
        {
            return _author;
        }

        public virtual bool CanEnable()
        {
            return true;
        }

        public virtual bool DefaultEnabled()
        {
            return true;
        }

        public virtual string Description()
        {
            return _description;
        }

        public virtual bool GetHome(out string strButtonText, out string strButtonImage, out string strButtonImageFocus, out string strPictureImage)
        {
            strButtonText = _name;
            strButtonImage = string.Empty;
            strButtonImageFocus = string.Empty;
            strPictureImage = string.Empty;
            return true;
        }

        public virtual int GetWindowId()
        {
            return _pluginId;
        }

        public virtual bool HasSetup()
        {
            return false;
        }

        public virtual string PluginName()
        {
            return _name;
        }

        public virtual void ShowPlugin()
        {
            return;
        }

        #endregion

        #region IPlugin

        public virtual void Start()
        {
            return;
        }

        public virtual void Stop()
        {
            return;
        }

        #endregion

    }
}
