using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pondman.MediaPortal;

namespace MediaPortal.GUI.Library
{
    /// <summary>
    /// Advanced version of the MediaPortal GUIWindow with support for serialized parameters
    /// </summary>
    /// <typeparam name="TParameters">The type of the parameters.</typeparam>
    public abstract class GUIWindowX<TParameters> : GUIWindowX
        where TParameters : class, new()
    {
        protected TParameters _parameters;
        
        protected GUIWindowX(string skinFile, int WindowId)
            : base(skinFile, WindowId)
        {
            _parameters = new TParameters();
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            if (!string.IsNullOrEmpty(_loadParameter))
            {
                try
                {
                    _parameters = Newtonsoft.Json.JsonConvert.DeserializeObject<TParameters>(_loadParameter);
                }
                catch (Exception e)
                {
                    Log.Error("Invalid loading parameters: {0}", e);
                    _parameters = new TParameters();
                }
            }
        }

        public virtual TParameters Parameters
        {
            get
            {
                return _parameters;
            }
        }

    }

    /// <summary>
    /// Advanced version of the MediaPortal GUIWindow
    /// </summary>
    public abstract class GUIWindowX : GUIWindow
    {
        #region Variables

        protected string _skinFile;
        protected string _publishPrefix;
        protected ILogger _logger;

        #endregion

        protected GUIWindowX(string name, int WindowId)
        {
            _skinFile = name + ".xml";
            _publishPrefix = "#" + name;
            GetID = WindowId;
            _logger = NullLogger.Instance;
        }

        /// <summary>
        /// Returns the active logger for this window.
        /// </summary>
        /// <value>
        /// the logger
        /// </value>
        protected ILogger Log
        {
            get 
            {
                return _logger;
            }
        }

        /// <summary>
        /// Initializes the window and loads the skin file.
        /// </summary>
        /// <returns></returns>
        public override bool Init()
        {
            Load(GUIGraphicsContext.Skin + @"\" + _skinFile);
     
            return base.Init();
        }

        protected virtual void Publish(string property, string value)
        {
            GUIUtils.Publish(_publishPrefix + property, value);
        }

        protected virtual void Unpublish()
        {
            GUIUtils.Unpublish(_publishPrefix);
        }

        protected virtual void Publish(object obj, params string[] exclude)
        {
            obj.Publish(_publishPrefix, exclude);
        }

        
    }
}
