using System.Collections.Generic;
using MediaBrowser.Model.Querying;

namespace Pondman.MediaPortal.MediaBrowser.Models
{
    public class SortableQuery
    {
        public SortableQuery()
        {
            Filters = new HashSet<ItemFilter>();
        }
        
        public int? Limit { get; set; }

        public int? Offset { get; set; }

        public string SortBy { get; set; }

        public bool? Descending { get; set; }

        public HashSet<ItemFilter> Filters { get; set; }
    }
}
