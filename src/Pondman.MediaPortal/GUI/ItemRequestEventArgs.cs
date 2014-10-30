using System;
using System.Collections.Generic;
using MediaPortal.GUI.Library;

namespace Pondman.MediaPortal.GUI
{
    public class ItemRequestEventArgs : EventArgs
    {
        public ItemRequestEventArgs(GUIListItem parent, int offset)
        {
            Parent = parent;
            List = new List<GUIListItem>();
            Offset = offset;
        }

        public GUIListItem Parent { get; private set; }

        public List<GUIListItem> List { get; private set; }

        public int? Selected { get; set; }
        
        public int Offset { get; private set; }

        public int TotalItems { get; set; }
    }
}