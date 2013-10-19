using System.Collections.Generic;
using MediaPortal.GUI.Library;

namespace Pondman.MediaPortal.GUI
{
    public class BrowserView<TIdentifier>
    {
        public BrowserView()
        {
            List = new List<GUIListItem>();
        } 
        
        public GUIListItem Parent { get; set; }

        public List<GUIListItem> List { get; set; }

        public int Offset { get; set; }

        public int Total { get; set; }

        public TIdentifier Selected { get; set; }

        public bool HasMore
        {
            get
            {
                return (List.Count < Total);
            }
        }
    }
}