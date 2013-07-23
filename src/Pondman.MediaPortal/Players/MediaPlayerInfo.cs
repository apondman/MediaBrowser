using MediaPortal.GUI.Library;
using MediaPortal.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Dispatcher;

namespace Pondman.MediaPortal
{

    public class MediaPlayerInfo
    {
        public MediaPlayerInfo(string path)
        {
            Path = path;
        }

        public string Title { get; set; }

        public string Year { get; set; }

        public string Plot { get; set; }

        public string Thumb { get; set; }

        public string Genre { get; set; }

        public string Path { get; internal set; }

    }

   
}