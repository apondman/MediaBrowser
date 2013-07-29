using System.Net.Configuration;
using MediaPortal.GUI.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Media.Animation;

namespace Pondman.MediaPortal.GUI
{
    public class BrowserPublishSettings
    {
        public BrowserPublishSettings()
        {
            Prefix = "#";
            Delay = 250;
        }
        
        public virtual string Prefix { get; set; }
        
        public virtual int Delay { get; set; }
    }
    
    public class GUIBrowser : GUIBrowser<int>
    {
        public GUIBrowser(ILogger logger = null)
            : base(x => x.ItemId, logger)
        {
        
        }
    }

    /// <summary>
    /// GUIListItem Browser
    /// </summary>
    /// <typeparam name="TIdentifier">The type of the identifier.</typeparam>
    public class GUIBrowser<TIdentifier>
    {
        protected BrowserPublishSettings _settings;
        protected ILogger _logger;
        protected GUIControl _control;
        protected GUIListItem _current;
        protected Func<GUIListItem, TIdentifier> _resolver;
        protected List<GUIListItem> _list;

        double _lastPublished = 0;
        Timer _publishTimer;

        public GUIBrowser(Func<GUIListItem, TIdentifier> resolver, ILogger logger = null)
        {
            _logger = logger ?? NullLogger.Instance;
            _list = new List<GUIListItem>();
            _settings = new BrowserPublishSettings();
            _resolver = resolver;            
        }

        /// <summary>
        /// Occurs when a browser item is selected.
        /// </summary>
        public event Action<GUIListItem> ItemSelected;

        /// <summary>
        /// Occurs when the current browser item changes.
        /// </summary>
        public event Action<GUIListItem> CurrentItemChanged;

        public event EventHandler PreloadRequested;

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
        /// Attaches GUIControl to this browser
        /// </summary>
        /// <param name="control">The control.</param>
        public virtual void Attach(GUIControl control)
        {
            // todo: attach logic (which control)
            _control = control;
        }

        /// <summary>
        /// Gets the browser settings.
        /// </summary>
        /// <value>
        /// The settings.
        /// </value>
        public BrowserPublishSettings Settings 
        {
            get
            {
                return _settings;
            }
        }
        
        public virtual bool IsPreloading { get; set; }

        /// <summary>
        /// Gets or sets the limit.
        /// </summary>
        /// <value>
        /// The limit.
        /// </value>
        public virtual int Limit { get; set; }

        /// <summary>
        /// Gets or sets the total count for the current list.
        /// </summary>
        /// <value>
        /// The total count.
        /// </value>
        public virtual int TotalCount { get; set;}

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>
        /// The count.
        /// </value>
        public virtual int Count
        {
            get
            {
                return _list.Count;
            }
        }

        /// <summary>
        /// Adds the specified item to the browser list.
        /// </summary>
        /// <param name="item">The item.</param>
        public virtual void Add(GUIListItem item)
        {
            item.OnItemSelected += OnItemSelected; 
            _list.Add(item);
        }

        /// <summary>
        /// Set current "owner" of the list items and clears the list
        /// </summary>
        /// <param name="item">The item.</param>
        public virtual void Current(GUIListItem item)
        {
            Clear();
            _current = item;
        }

        /// <summary>
        /// Clears the browser list.
        /// </summary>
        public virtual void Clear()
        {
            _list.Clear();
            TotalCount = 0;
        }

        protected void OnItemSelected(GUIListItem item, GUIControl parent)
        {
            var facade = _control as GUIFacadeControl;

            if (!facade.IsRelated(parent) || facade != null && facade.SelectedListItem != item)
                return;

            if (Limit > 0 && !IsPreloading && facade.SelectedListItemIndex > facade.Count - (int)(Limit / 2) && facade.Count < TotalCount)
                PreloadRequested.FireEvent(this, EventArgs.Empty);

            DelayedItemHandler(item);
        }

        /// <summary>
        /// Publishes all the items to the attached control.
        /// </summary>
        /// <param name="selected">identifier for the item that needs to be selected</param>
        public virtual void Publish(TIdentifier selected)
        {
            if (_control is GUIFacadeControl)
            {
                Publish(_control as GUIFacadeControl, selected);
            }

            // Publish current item
            if (CurrentItemChanged != null) 
            {
                CurrentItemChanged(_current);
            }

            _control.Focus();
        }

        public virtual void Continue()
        {
            if (!(_control is GUIFacadeControl)) return;
            var facade = _control as GUIFacadeControl;
            Append(facade, default(TIdentifier), false);
        }

        protected virtual void Publish(GUIFacadeControl facade, TIdentifier selected) 
        {
            // set the layout just to make sure properties are being set.
            facade.CurrentLayout = facade.CurrentLayout;
            facade.ClearAll();
            
            Append(facade, selected);
        }

        protected virtual void Append(GUIFacadeControl facade, TIdentifier selected, bool reselect = true)
        {
            IsPreloading = false;
            for (var i = facade.Count; i < _list.Count; i++)
            {
                var item = _list[i];
                facade.Add(item);

                if (!reselect || !GetKeyForItem(item).Equals(selected)) continue;
                facade.SelectIndex(i);
                reselect = false;
            }

            if (reselect)
            {
                // select the first item to trigger labels
                facade.SelectIndex(0);
            }

            // Update Total Count if it was not done manually
            if (TotalCount == 0)
            {
                TotalCount = _list.Count;
            }

            _list.Count.Publish(_settings.Prefix + ".Browser.Items.Current");
            TotalCount.Publish(_settings.Prefix + ".Browser.Items.Total");
            // todo: add filtered count
        }

        protected TIdentifier GetKeyForItem(GUIListItem item)
        {
            return _resolver(item);
        }

        protected void DelayedItemHandler(GUIListItem item)
        {
            double tickCount = AnimationTimer.TickCount;

            // Publish instantly when previous request has passed the required delay
            if (_settings.Delay < (int)(tickCount - _lastPublished))
            {
                _lastPublished = tickCount;
                ItemSelected.BeginInvoke(item, ItemSelected.EndInvoke, null);
                return;
            }

            _lastPublished = tickCount;
            if (_publishTimer == null)
            {
                _publishTimer = new Timer(x => ItemSelected(((GUIFacadeControl)_control).SelectedListItem), null, _settings.Delay, Timeout.Infinite);
            }
            else
            {
                _publishTimer.Change(_settings.Delay, Timeout.Infinite);
            }
        }

    }
}
