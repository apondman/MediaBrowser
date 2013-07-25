using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MediaBrowser.ApiInteraction.net35;
using MediaBrowser.Model.Dto;
using System.Linq.Expressions;

namespace Pondman.MediaPortal.MediaBrowser
{
    /// <summary>
    /// MediaBrowser Query Helper
    /// </summary>
    public static class MediaBrowserQueries
    {

        /// <summary>
        /// Create a new item query
        /// </summary>
        /// <value>
        /// The new query instance.
        /// </value>
        public static ItemQuery New
        {
            get 
            {
                return new ItemQuery();
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

        public static ItemQuery SortBy<TProperty>(this ItemQuery query, Expression<Func<BaseItemDto, TProperty>> property)
        {
            return query.SortBy(GetParameterName(property));
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

        /// <summary>
        /// Add filters to the query
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="filters">The filters.</param>
        /// <returns></returns>
        public static ItemQuery Filters(this ItemQuery query, params ItemFilter[] filters)
        {
            query.Filters = query.Filters.Concat(filters).ToArray();
            return query;
        }

        /// <summary>
        /// Sort ascending.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public static ItemQuery Ascending(this ItemQuery query)
        {
            query.SortOrder = SortOrder.Ascending;
            return query;
        }

        /// <summary>
        /// Sort descending.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public static ItemQuery Descending(this ItemQuery query)
        {
            query.SortOrder = SortOrder.Descending;
            return query;
        }

        /// <summary>
        /// Include extra fields
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="fields">The fields to include</param>
        /// <returns></returns>
        public static ItemQuery Fields(this ItemQuery query, params ItemFields[] fields)
        {
            query.Fields = query.Fields.Concat(fields).ToArray();
            return query;
        }

        /// <summary>
        /// Makes the query resursive
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        /// <returns></returns>
        public static ItemQuery Recursive(this ItemQuery query, bool value = true)
        {
            query.Recursive = value;
            return query;
        }

        /// <summary>
        /// Limits the result of the query to the specified item count.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="count">Limit</param>
        /// <returns></returns>
        public static ItemQuery Limit(this ItemQuery query, int? count)
        {
            query.Limit = count;
            return query;
        }

        private static string GetParameterName(Expression reference)
        {
            var lambda = reference as LambdaExpression;
            var member = lambda.Body as MemberExpression;

            return member.Member.Name;
        }
    }

}
