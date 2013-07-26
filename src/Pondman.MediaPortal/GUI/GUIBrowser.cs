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

        double lastPublished = 0;
        Timer publishTimer;

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

        /// <summary>
        /// Occurs when the next page is requested.
        /// </summary>
        public event EventHandler NextPage;

        /// <summary>
        /// Occurs when the previous page is requested.
        /// </summary>
        public event EventHandler PreviousPage;

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

        /// <summary>
        /// Gets or sets the offset.
        /// </summary>
        /// <value>
        /// The offset.
        /// </value>
        public virtual int Offset { get; set; }
        
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
        /// Adds the specified item to the browser list.
        /// </summary>
        /// <param name="item">The item.</param>
        public virtual void Add(GUIListItem item)
        {
            item.OnItemSelected += item_OnItemSelected; 
            _list.Add(item);
        }

        /// <summary>
        /// Set current "owner" of the list items
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
            Offset = 0;
            Limit = 0;
            TotalCount = 0;
        }

        /// <summary>
        /// Returns true when the browser has handled the click event.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public virtual bool OnClicked(GUIListItem item)
        {
            switch (item.ItemId)
            {
                case 10000:
                    NextPage.FireEvent(this, EventArgs.Empty);
                    return true;
                case 10001:
                    PreviousPage.FireEvent(this, EventArgs.Empty);
                    return true;
                default:
                    return false;
            }
        }

        void item_OnItemSelected(GUIListItem item, GUIControl parent)
        {
            var facade = _control as GUIFacadeControl;

            if (!facade.IsRelated(parent) || facade != null & facade.SelectedListItem != item)
                return;

            if (item.ItemId == 10000 || item.ItemId == 10001)
                return;

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

            // Update Total Count if it was not done manually
            if (TotalCount == 0)
            {
                TotalCount = _list.Count;
            }

            // Publish current item
            if (CurrentItemChanged != null) 
            {
                CurrentItemChanged(_current);
            }

            _list.Count.Publish(_settings.Prefix + ".Browser.Items.Current");
            TotalCount.Publish(_settings.Prefix + ".Browser.Items.Total");
            // todo: add filtered count

            _control.Focus();
        }

        protected virtual void Publish(GUIFacadeControl facade, TIdentifier selected) 
        {
            bool first = true;

            // set the layout just to make sure properties are being set.
            facade.CurrentLayout = facade.CurrentLayout;
            facade.ClearAll();
            
            for(int i=0;i<_list.Count;i++)
            {
                var item = _list[i];
                facade.Add(item);
                if (selected != null && GetKeyForItem(item).Equals(selected))
                {
                    first = false;
                    facade.SelectIndex(i);
                }
            }

            if (first)
            {
                // select the first item to trigger labels
                facade.SelectIndex(0);
            }
        }

        protected TIdentifier GetKeyForItem(GUIListItem item)
        {
            return _resolver(item);
        }

        protected void DelayedItemHandler(GUIListItem item)
        {
            double tickCount = AnimationTimer.TickCount;

            // Publish instantly when previous request has passed the required delay
            if (_settings.Delay < (int)(tickCount - lastPublished))
            {
                lastPublished = tickCount;
                ItemSelected.BeginInvoke(item, ItemSelected.EndInvoke, null);
                return;
            }

            lastPublished = tickCount;
            if (publishTimer == null)
            {
                publishTimer = new Timer(x => ItemSelected(((GUIFacadeControl)_control).SelectedListItem), null, _settings.Delay, Timeout.Infinite);
            }
            else
            {
                publishTimer.Change(_settings.Delay, Timeout.Infinite);
            }
        }

        protected virtual GUIListItem NextPageItem(string label)
        {
            return SpecialItem(label, 10000);
        }

        protected virtual GUIListItem PreviousPageItem(string label)
        {
            return SpecialItem(label, 10001);
        }

        protected virtual GUIListItem SpecialItem(string label, int id)
        {
            return new GUIListItem {Label = label, IsFolder = true, ItemId = id};
        }
    }
}
