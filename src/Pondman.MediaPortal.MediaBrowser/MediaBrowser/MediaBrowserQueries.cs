﻿using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace Pondman.MediaPortal.MediaBrowser
{
    using Pondman.MediaPortal.MediaBrowser.Models;
    using System;
    using System.Linq;
    using System.Linq.Expressions;

    /// <summary>
    /// MediaBrowser Query Helper
    /// </summary>
    public static class MediaBrowserQueries
    {
        public static ItemQuery Missing(this ItemQuery query, bool value = true)
        {
            query.IsMissing = value;
            return query;
        }

        public static ItemQuery Unaired(this ItemQuery query, bool value = true)
        {
            query.IsUnaired = value;
            return query;
        }         

        public static ItemQuery Rated(this ItemQuery query, bool value = true)
        {
            query.HasParentalRating = value;
            return query;
        } 
        
        /// <summary>
        /// Returns a randomized item query
        /// </summary>
        /// <returns></returns>
        public static ItemQuery Random
        {
            get
            {
                return MediaBrowserQueries.Item.Recursive().Limit(1).SortBy(ItemSortBy.Random);
            }
        }

        /// <summary>
        /// Returns a randomized movie query
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <returns></returns>
        public static ItemQuery RandomMovie(string userId)
        {
            return Random.Movies().UserId(userId);
        }

        /// <summary>
        /// Create a new item query
        /// </summary>
        /// <value>
        /// The new query instance.
        /// </value>
        public static ItemQuery Item
        {
            get 
            {
                return new ItemQuery();
            }            
        }

        /// <summary>
        /// Gets the name of the item by.
        /// </summary>
        /// <value>
        /// The name of the item by.
        /// </value>
        public static ItemsByNameQuery Named
        {
           get 
            {
                return new ItemsByNameQuery().Recursive();
            }  
        }

        public static PersonsQuery Persons
        {
            get
            {
                return new PersonsQuery().Recursive();
            }
        }

        public static NextUpQuery NextUp
        {
            get
            {
                return new NextUpQuery();
            }
        }

        public static ItemQuery IndexContainers(this ItemQuery query, bool include = true)
        {
            query.IncludeIndexContainers = include;
            return query;
        }


        /// <summary>
        /// Include Box Sets in the current query
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public static ItemQuery Collections(this ItemQuery query)
        {
            return query.IncludeItemTypes("BoxSet");
        }

        public static ItemQuery Audio(this ItemQuery query)
        {
            return query.IncludeItemTypes("Audio");
        }

        public static ItemQuery MusicAlbum(this ItemQuery query)
        {
            return query.IncludeItemTypes("MusicAlbum");
        }

        /// <summary>
        /// Include TVShows in the current query
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public static ItemQuery Series(this ItemQuery query)
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
            return query.IncludeItemTypes(MediaBrowserType.Movie);
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

        public static ItemQuery Search(this ItemQuery query, string term)
        {
            query.SearchTerm = term;
            return query;
        }

        public static ItemQuery Season(this ItemQuery query)
        {
            return query.IncludeItemTypes("Season");
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
        /// Filter items by parent id
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="parentId">The parent id.</param>
        /// <returns></returns>
        public static ItemQuery ParentId(this ItemQuery query, string parentId)
        {
            query.ParentId = parentId;
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
        /// Sort the query using the specified property
        /// </summary>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="property">The property.</param>
        /// <returns></returns>
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

        public static NextUpQuery User(this NextUpQuery query, string userId)
        {
            query.UserId = userId;
            return query;
        }

        public static NextUpQuery Limit(this NextUpQuery query, int? limit)
        {
            query.Limit = limit;
            return query;
        }

        public static NextUpQuery Fields(this NextUpQuery query, params ItemFields[] fields)
        {
            query.Fields = (query.Fields ?? new ItemFields[]{}).Concat(fields).ToArray();
            return query;
        }

        /// <summary>
        /// Users the specified query.
        /// </summary>
        /// <typeparam name="TNamedQuery">The type of the named query.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="userId">The user identifier.</param>
        /// <returns></returns>
        public static TNamedQuery User<TNamedQuery>(this TNamedQuery query, string userId) where TNamedQuery : ItemsByNameQuery
        {
            query.UserId = userId;
            return query;
        }

        /// <summary>
        /// Includes the specified query.
        /// </summary>
        /// <typeparam name="TNamedQuery">The type of the named query.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="includeItemTypes">The include item types.</param>
        /// <returns></returns>
        public static TNamedQuery Include<TNamedQuery>(this TNamedQuery query, params string[] includeItemTypes) where TNamedQuery : ItemsByNameQuery
        {
            query.IncludeItemTypes = includeItemTypes;
            return query;
        }

        public static TNamedQuery Fields<TNamedQuery>(this TNamedQuery query, params ItemFields[] fields) where TNamedQuery : ItemsByNameQuery
        {
            query.Fields = (query.Fields ?? new ItemFields[] { }).Concat(fields).ToArray();
            return query;
        }

        /// <summary>
        /// Sort ascending.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public static TNamedQuery Ascending<TNamedQuery>(this TNamedQuery query) where TNamedQuery : ItemsByNameQuery
        {
            query.SortOrder = SortOrder.Ascending;
            return query;
        }

        /// <summary>
        /// Sort descending.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public static TNamedQuery Descending<TNamedQuery>(this TNamedQuery query) where TNamedQuery : ItemsByNameQuery
        {
            query.SortOrder = SortOrder.Descending;
            return query;
        }

        /// <summary>
        /// Sorts the by.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="fields">The fields.</param>
        /// <returns></returns>
        public static TNamedQuery SortBy<TNamedQuery>(this TNamedQuery query, params string[] fields) where TNamedQuery : ItemsByNameQuery
        {
            query.SortBy = query.SortBy.Concat(fields).ToArray();
            return query;
        }

        /// <summary>
        /// Makes the query resursive
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        /// <returns></returns>
        public static TNamedQuery Recursive<TNamedQuery>(this TNamedQuery query, bool value = true) where TNamedQuery : ItemsByNameQuery
        {
            query.Recursive = value;
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
            query.Fields = (query.Fields ?? new ItemFields[]{}).Concat(fields).ToArray();
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

        /// <summary>
        /// Filters the query by genre.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="genres">The genres.</param>
        /// <returns></returns>
        public static ItemQuery Genres(this ItemQuery query, params string[] genres)
        {
            query.Genres = query.Genres.Concat(genres).ToArray();
            return query;
        }

        /// <summary>
        /// Filters the query by studio.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="studios">The studios.</param>
        /// <returns></returns>
        public static ItemQuery Studios(this ItemQuery query, params string[] studios)
        {
            query.Studios = query.Studios.Concat(studios).ToArray();
            return query;
        }

        /// <summary>
        /// Filters the query by artists.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="artists">The artists.</param>
        /// <returns></returns>
        public static ItemQuery Artists(this ItemQuery query, params string[] artists)
        {
            query.Artists = (query.Artists ?? new string[]{}).Concat(artists).ToArray();
            return query;
        }

        /// <summary>
        /// Filters the query by person.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="person">The person.</param>
        /// <returns></returns>
        public static ItemQuery Person(this ItemQuery query, string person)
        {
            query.Person = person;
            return query;
        }

        /// <summary>
        /// Filters the query by watched items.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="isWatched">if set to <c>true</c> [is watched].</param>
        /// <returns></returns>
        public static ItemQuery Watched(this ItemQuery query, bool isWatched = true)
        {
            query.Filters(isWatched ? ItemFilter.IsPlayed : ItemFilter.IsUnplayed);
            return query;
        }

        public static ItemQuery Apply(this ItemQuery query, SortableQuery options)
        {
            query.StartIndex = options.Offset;
            query.Limit = options.Limit;
            if (options.SortBy != null)
            {
                query.SortBy(options.SortBy);
            }
            if (options.Descending.HasValue)
            {
                query.SortOrder = options.Descending.Value ? SortOrder.Descending : SortOrder.Ascending;
            }

            if (query.SortBy == null || query.SortBy.Length == 0)
            {
                query.SortBy(ItemSortBy.SortName).Ascending();
            }

            if (options.Filters.Count > 0)
            {
                options.Filters.ToList().ForEach(x => query.Filters(x));
            }

            if (!options.StartsWith.IsIn(null, "#"))
            {
                query.NameStartsWithOrGreater = options.StartsWith;
            }

            return query;
        }

        public static TNamedQuery Apply<TNamedQuery>(this TNamedQuery query, SortableQuery options) where TNamedQuery : ItemsByNameQuery
        {
            query.StartIndex = options.Offset;
            query.Limit = options.Limit;
            if (options.SortBy != null)
            {
                query.SortBy(options.SortBy);
            }
            if (options.Descending.HasValue)
            {
                query.SortOrder = options.Descending.Value ? SortOrder.Descending : SortOrder.Ascending;
            }

            if (query.SortBy == null || query.SortBy.Length == 0)
            {
                query.SortBy(ItemSortBy.SortName).Ascending();
            }

            return query;
        }

        public static PersonsQuery Apply(this PersonsQuery query, SortableQuery options)
        {
            if (options.PersonTypes.Count > 0)
            {
                options.PersonTypes.ToList().ForEach(x => query.PersonTypes(x));
            }

            return query.Apply<PersonsQuery>(options);
        }

        public static PersonsQuery PersonTypes(this PersonsQuery query, params string[] types)
        {
            query.PersonTypes = query.PersonTypes.Concat(types).ToArray();
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
