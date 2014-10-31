using System.Collections.Generic;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace Pondman.MediaPortal.MediaBrowser.Models
{
    public class SortableQuery
    {
        public SortableQuery()
        {
            Filters = new HashSet<ItemFilter>();
            PersonTypes = new HashSet<string>();
            SortBy = ItemSortBy.SortName;
        }

        public int? Limit { get; set; }

        public int? Offset { get; set; }

        public string StartsWith { get; set; }

        public string SortBy { get; set; }

        public bool? Descending { get; set; }

        public HashSet<ItemFilter> Filters { get; set; }

        public HashSet<string> PersonTypes { get; set; }
    }
}
