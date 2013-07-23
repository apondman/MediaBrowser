using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MediaBrowser.ApiInteraction.net35;

namespace Pondman.MediaPortal.MediaBrowser
{
    /// <summary>
    /// MediaBrowser Query Helper
    /// </summary>
    public static class MediaBrowserQueries
    {

        /// <summary>
        /// Create new default query
        /// </summary>
        /// <value>
        /// The new.
        /// </value>
        public static ItemQuery New
        {
            get 
            {
                var query = new ItemQuery();
                //query.Limit = 250;
                query.Recursive = true;
                query.SortBy(ItemSortBy.SortName);
                query.SortOrder = SortOrder.Ascending;
                return query;
            }            
        }

        /// <summary>
        /// Include TVShows in the current query
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public static ItemQuery TVShows(this ItemQuery query)
        {
            return query.IncludeItemTypes("Series");
        }

        /// <summary>
        /// Include movies in the current query
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public static ItemQuery Movies(this ItemQuery query)
        {
            return query.IncludeItemTypes("Movie");
        }

        /// <summary>
        /// Include episodes in the current query
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public static ItemQuery Episode(this ItemQuery query)
        {
            return query.IncludeItemTypes("Episode");
        }

        /// <summary>
        /// Filter items by user id
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="userId">The user id.</param>
        /// <returns></returns>
        public static ItemQuery UserId(this ItemQuery query, string userId)
        {
            query.UserId = userId;
            return query;
        }

        /// <summary>
        /// Shorthand to includes the item types this query should return.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="types">The types.</param>
        public static ItemQuery IncludeItemTypes(this ItemQuery query, params string[] types)
        {
            query.IncludeItemTypes = query.IncludeItemTypes.Concat(types).ToArray();
            return query;
        }

        /// <summary>
        /// Sort this queries by the specified fields
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="fields">The fields.</param>
        /// <returns></returns>
        public static ItemQuery SortBy(this ItemQuery query, params string[] fields)
        {
            query.SortBy = query.SortBy.Concat(fields).ToArray();
            return query;
        }

        public static ItemQuery Fields(this ItemQuery query, params ItemFields[] fields)
        {
            query.Fields = query.Fields.Concat(fields).ToArray();
            return query;


        }
    }

}
