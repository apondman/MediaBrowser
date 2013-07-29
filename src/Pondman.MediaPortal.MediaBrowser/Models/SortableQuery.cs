using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pondman.MediaPortal.MediaBrowser.Models
{
    public class SortableQuery
    {
        public int? Limit { get; set; }

        public int? Offset { get; set; }

        public string SortBy { get; set; }

        public bool? Descending { get; set; }
    }
}
