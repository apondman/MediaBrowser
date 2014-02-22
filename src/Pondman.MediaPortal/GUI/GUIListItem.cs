using MediaPortal.GUI.Library;

namespace Pondman.MediaPortal.GUI
{
    public class GUIListItem<TInstance> : GUIListItem
    {
        public TInstance Tag { get; set; }
    }
}
