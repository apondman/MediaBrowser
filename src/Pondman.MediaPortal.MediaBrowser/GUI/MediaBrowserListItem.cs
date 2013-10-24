using MediaBrowser.Model.Dto;
using MediaPortal.GUI.Library;

namespace Pondman.MediaPortal.MediaBrowser.GUI
{
    public class MediaBrowserListItem : GUIListItem
    {
        public BaseItemDto Dto
        {
            get
            {
                return TVTag as BaseItemDto;
            }
        }
    }
}
