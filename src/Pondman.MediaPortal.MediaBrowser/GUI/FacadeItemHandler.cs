using MediaBrowser.Model.Dto;
using MediaPortal.GUI.Library;

namespace Pondman.MediaPortal.MediaBrowser.GUI
{
    using System;
    using System.Threading;
    using System.Windows.Media.Animation;    
    
    public class FacadeItemHandler : IDisposable
    {
        private readonly Action<GUIListItem> _itemSelected;
        private Timer _timer;
        private bool _disposed;
        private double _lastPublishedTicks;
        private readonly GUIFacadeControl _facade;

        public FacadeItemHandler(GUIFacadeControl facade)
        {
            _facade = facade;
            _itemSelected = OnItemSelected;
        }

        /// <summary>
        /// Gets or sets the skin property.
        /// </summary>
        /// <value>
        /// The property.
        /// </value>
        public string Property { get; set; }

        public void SetLoading(bool isLoading)
        {
            isLoading.Publish(Property + ".Loading");
        }

        public void DelayedItemHandler(GUIListItem item, GUIControl parent)
        {
            double tickCount = AnimationTimer.TickCount;
            int delay = MediaBrowserPlugin.Config.Settings.PublishDelayMs;

            // Publish instantly when previous request has passed the required delay
            if (delay < (int)(tickCount - _lastPublishedTicks))
            {
                _lastPublishedTicks = tickCount;
                _itemSelected.BeginInvoke(item, _itemSelected.EndInvoke, null);
                return;
            }

            _lastPublishedTicks = tickCount;
            if (_timer == null)
            {
                _timer = new Timer(x => _itemSelected(_facade.SelectedListItem), null, delay, Timeout.Infinite);
            }
            else
            {
                _timer.Change(delay, Timeout.Infinite);
            }
        }

        void OnItemSelected(GUIListItem item)
        {
            if (item == null) return;

            var dto = item.TVTag as BaseItemDto;
            dto.IfNotNull(x => x.Publish(Property + ".Selected"));
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
                if (_timer != null)
                    _timer.Dispose();
            }

            _disposed = true;
        }
    }
}
