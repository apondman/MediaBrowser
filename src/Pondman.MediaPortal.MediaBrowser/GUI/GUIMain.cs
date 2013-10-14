using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Search;
using MediaPortal.GUI.Library;
using Pondman.MediaPortal.GUI;
using Pondman.MediaPortal.MediaBrowser.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MPGui = MediaPortal.GUI.Library;

namespace Pondman.MediaPortal.MediaBrowser.GUI
{
    /// <summary>
    ///     Main Browser
    /// </summary>
    public class GUIMain : GUIDefault
    {
        private readonly GUIBrowser<string> _browser;
        private readonly List<GUIListItem> _filters;
        private readonly Dictionary<string, GUIFacadeControl> _facades;
        private SortableQuery _sortableQuery;

        public GUIMain()
            : base(MediaBrowserWindow.Main)
        {
            _browser = new GUIBrowser<string>(GetIdentifier, MediaBrowserPlugin.Log);
            _browser.Settings.Prefix = MediaBrowserPlugin.DefaultProperty;
            _browser.Settings.LoadingPlaceholderLabel = MediaBrowserPlugin.UI.Resource.LoadingMoreItems;
            _browser.ItemSelected += OnBaseItemSelected;
            _browser.ItemPublished += OnItemPublished;
            _browser.ItemChanged += OnItemChanged;
            _browser.ItemsRequested += OnItemsRequested;

            _filters = new List<GUIListItem>();
            _facades = new Dictionary<string, GUIFacadeControl>();

            // register commands
            RegisterCommand("CycleLayout", CycleLayoutCommand);
            RegisterCommand("Sort", SortCommand);
            RegisterCommand("Filter", FilterCommand);
            RegisterCommand("Search", SearchCommand);
        }

        #region Controls

        protected GUIFacadeControl Facade = null;

        #endregion

        #region Commands

        /// <summary>
        ///     Cycles layout modes for the facade.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="actionType">Type of the action.</param>
        protected void CycleLayoutCommand(GUIControl control, MPGui.Action.ActionType actionType)
        {
            Facade.CycleLayout();
            Facade.Focus();
             MediaBrowserPlugin.Config.Settings
                    .ForUser(GUIContext.Instance.ActiveUser.Id)
                    .ForContext(CurrentItem.GetContext())
                    .Layout = Facade.CurrentLayout;
            Log.Debug("Cycle Layout Result: {0}", Facade.CurrentLayout);
        }

        /// <summary>
        ///     Sort the current view
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="actionType">Type of the action.</param>
        protected void SortCommand(GUIControl control, MPGui.Action.ActionType actionType)
        {
            ShowSortMenuDialog();
        }

        /// <summary>
        ///     Shows the filter menu dialog.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="actionType">Type of the action.</param>
        protected void FilterCommand(GUIControl control, MPGui.Action.ActionType actionType)
        {
            ShowFilterMenuDialog();
        }

        protected void SearchCommand(GUIControl control, MPGui.Action.ActionType actionType)
        {
            ShowSearchDialog();
        }

        #endregion

        #region Window overrides

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            if (!GUIContext.Instance.IsServerReady || !GUIContext.Instance.Client.IsUserLoggedIn) return;

            // no facades!
            if (_facades.Count == 0)
            {
                // todo: dialog?
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            // update browser settings
            _browser.Settings.Limit = MediaBrowserPlugin.Config.Settings.DefaultItemLimit;

            // browse to item
            if (!String.IsNullOrEmpty(Parameters.Id))
            {
                if (Parameters.Type == "View")
                {
                    var item = GetViewListItem(Parameters.Id);
                    Navigate(item);
                }
                else
                {
                    GUIContext.Instance.Client.GetItem(Parameters.Id, GUIContext.Instance.Client.CurrentUserId, LoadItem,
                        ShowItemsError);
                }
                return;
            }

            // reload what we have
            _browser.Reload();
        }

        protected override void OnWindowLoaded()
        {
            base.OnWindowLoaded();

            if (controlList != null)
                controlList
                    .OfType<GUIFacadeControl>()
                    .Where(x => x.Description.StartsWith("MediaBrowser.Facade."))
                    .Select(x => new { facade = x, name = x.Description.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries)[2] })
                    .ToList()
                    .ForEach(x => _facades[x.name] = x.facade);
        }

        protected override void OnClicked(int controlId, GUIControl control, MPGui.Action.ActionType actionType)
        {
            if (_browser.IsBusy)
            {
                return;
            }

            if (Facade == control)
            {
                switch (actionType)
                {
                    case MPGui.Action.ActionType.ACTION_SELECT_ITEM:

                        // reset sortable query
                        // todo: bad place
                        _sortableQuery = new SortableQuery();

                        Navigate(Facade.SelectedListItem);
                        return;
                }
            }
            

            base.OnClicked(controlId, control, actionType);
        }

        public override void OnAction(MPGui.Action action)
        {
            switch (action.wID)
            {
                case MPGui.Action.ActionType.ACTION_PARENT_DIR:
                    // reset sortable query
                    // todo: bad place
                    _sortableQuery = new SortableQuery();
                    OnPreviousWindow();
                    break;
                default:
                    base.OnAction(action);
                    break;
            }
        }

        protected override void OnShowContextMenu()
        {
            // show the profile selection dialog for now
            // ShowUserProfilesDialog();
            //ShowSortMenuDialog();
            //ShowSearchDialog();
            ShowFilterMenuDialog();
        }

        /// <summary>
        ///     Navigate back
        /// </summary>
        protected override void OnPreviousWindow()
        {
            // if we are in the root go to the previous window
            if (!_browser.Back())
            {
                base.OnPreviousWindow();
            }
        }

        #endregion

        #region GUIListItem Handlers

        /// <summary>
        ///     Handler for selected item
        /// </summary>
        /// <param name="item">The item.</param>
        protected void OnBaseItemSelected(GUIListItem item)
        {
            if (item == null) return;

            // todo: check for specific dto
            var dto = item.TVTag as BaseItemDto;
            dto.IfNotNull(x => PublishItemDetails(x, MediaBrowserPlugin.DefaultProperty + ".Selected"));
        }

        /// <summary>
        /// Called when parent item is changed
        /// </summary>
        /// <param name="item">The item.</param>
        protected void OnItemChanged(GUIListItem item)
        {
            if (item == null || !(item.TVTag is BaseItemDto)) return;

            var facade = GetTypedFacade(item.TVTag as BaseItemDto);
            
            if (facade != Facade)
            {
                // Attach the facade to the browser
                _browser.Attach(facade);
            }
        }

        protected void OnItemPublished(GUIListItem item)
        {
            if (item == null || !(item.TVTag is BaseItemDto)) return;

            var dto = item.TVTag as BaseItemDto;
            var facade = GetTypedFacade(dto);

            // read layout from settings
            var savedLayout = MediaBrowserPlugin.Config.Settings
                    .ForUser(GUIContext.Instance.ActiveUser.Id)
                    .ForContext(dto.GetContext()).Layout;

            // apply layout if different
            if (savedLayout.HasValue && facade.CurrentLayout != savedLayout)
            {
                facade.CurrentLayout = savedLayout.Value;
            }

            // if the facade changed
            if (facade != Facade)
            {
                Log.Debug("Active Facade: Name={0}, Id={1}", facade.Description, facade.GetID);
                
                // Hide current facade (if it is not null)
                Facade.IfNotNull(f => f.Visible(false));

                // Replace active facade
                Facade = facade;

                // Show new facade
                Facade.Visible(true);
            }

            CurrentItem = dto;
        }

        GUIFacadeControl GetTypedFacade(BaseItemDto dto)
        {
            GUIFacadeControl facade;
            // try typed facade
            if (!_facades.TryGetValue(dto.Type, out facade))
            {
                // try the Default facade
                if (!_facades.TryGetValue("Default", out facade))
                {
                    // just pick the first one 
                    facade = _facades.First().Value;
                }
            }

            return facade;
        }

        #endregion

        #region GUIListItem Factory Methods

        /// <summary>
        ///     Gets the view list item.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="label">The label.</param>
        /// <returns></returns>
        public GUIListItem GetViewListItem(string id, string label = null)
        {
            var view = new BaseItemDto {Name = label ?? id, Id = id, Type = "View", IsFolder = true};

            return GetBaseListItem(view);
        }

        /// <summary>
        ///     Gets the base list item.
        /// </summary>
        /// <param name="dto">The dto.</param>
        /// <returns></returns>
        public GUIListItem GetBaseListItem(BaseItemDto dto, BaseItemDto context = null)
        {
            var item = new GUIListItem(dto.Name)
            {
                Path = dto.GetContext(),
                Year = dto.ProductionYear.GetValueOrDefault(),
                TVTag = dto,
                IsFolder = dto.IsFolder,
                IconImage = "defaultVideo.png",
                IconImageBig = "defaultVideoBig.png",
                RetrieveArt = true,
                IsPlayed = dto.UserData != null && dto.UserData.Played,
            };
            item.OnRetrieveArt += GetItemImage;
            
            switch (dto.Type)
            {
                case "Audio":
                    item.Label2 = dto.Artists != null ? String.Join(",", dto.Artists.ToArray()) : "Unknown";
                    break;
                case "Episode":

                    if (context != null && context.Id == "tvshows-nextup")
                    {
                        item.Label = dto.SeriesName + String.Format(" - {0}x{1} - {2}", dto.ParentIndexNumber ?? 0, dto.IndexNumber ?? 0, item.Label);
                    }
                    else
                    {
                        item.Label = String.Format("{0}: {1}", dto.IndexNumber ?? 0, item.Label);
                    }
                        item.Label2 = dto.PremiereDate.HasValue
                            ? dto.PremiereDate.Value.ToString(GUIUtils.Culture.DateTimeFormat.ShortDatePattern)
                            : string.Empty;
                    break;
                case "Series":
                    item.Label2 = dto.ProductionYear.HasValue ? dto.ProductionYear.ToString() : string.Empty;
                    break;
                case "Movie":
                    item.Label2 = dto.ProductionYear.HasValue ? dto.ProductionYear.ToString() : string.Empty;
                    break;
                case "Season":
                case "BoxSet":
                    item.Label2 = dto.ChildCount.HasValue ? dto.ChildCount.ToString() : string.Empty;
                    break;
                case "Studio":
                case "Genre":
                    if (context != null && context.Id.StartsWith("tvshows"))
                    {
                        item.Label2 = (dto.SeriesCount ?? 0).ToString();
                    }
                    else
                    {
                        item.Label2 = (dto.MovieCount ?? 0).ToString();
                    }
                    break;
            }
            return item;
        }

       

        #endregion

        /// <summary>
        ///     Gets or sets the current item.
        /// </summary>
        /// <value>
        ///     The current item.
        /// </value>
        public BaseItemDto CurrentItem {
            get { return _currentItem; }
            set { _currentItem = value; OnCurrentItemChanged();}
        }private BaseItemDto _currentItem;

        private void OnCurrentItemChanged()
        {
            _filters.Clear();

            if (_currentItem == null) return;

            GUIContext.Instance.Update(_currentItem); // todo: context?
            PublishItemDetails(_currentItem, MediaBrowserPlugin.DefaultProperty + ".Current");

            if (_currentItem.Id.Contains("root-") || _currentItem.Type == "UserRootFolder") return;

            _filters.Add(GetFilterItem(ItemFilter.IsFavorite));
            _filters.Add(GetFilterItem(ItemFilter.Likes));
            _filters.Add(GetFilterItem(ItemFilter.Dislikes));

            if (_currentItem.Type.IsIn("Genre", "Studio")) return;

            _filters.Add(GetFilterItem(ItemFilter.IsPlayed));
            _filters.Add(GetFilterItem(ItemFilter.IsUnplayed));
            _filters.Add(GetFilterItem(ItemFilter.IsResumable));
        }

        /// <summary>
        ///     Resets the  navigation to the starting position
        /// </summary>
        protected override void Reset()
        {
            // execute base reset
            base.Reset();

            // execute window reset
            _browser.Reset();
            _sortableQuery = new SortableQuery();

            // get root folder
            GUIContext.Instance.Client.GetRootFolder(GUIContext.Instance.Client.CurrentUserId, LoadItem, ShowItemsError);
        }

        private void OnItemsRequested(object sender, ItemRequestEventArgs e)
        {
            var item = e.Parent.TVTag as BaseItemDto;
            if (item == null) return;

            Log.Debug("ItemsRequested()");

            WaitFor(x =>
            {
                // todo: this is a mess, rethink
                var userId = GUIContext.Instance.Client.CurrentUserId;
                var query = MediaBrowserQueries.Item
                                .UserId(userId)
                                .Recursive()
                                .Fields(ItemFields.Overview, ItemFields.People, ItemFields.Genres, ItemFields.MediaStreams);

                if (_browser.Settings.Limit > 0)
                {
                    _sortableQuery.Limit = _browser.Settings.Limit;
                    _sortableQuery.Offset = e.Offset;
                }

                Log.Debug("GetItems: Type={0}, Id={1}", item.Type, item.Id);

                switch (item.Type)
                {
                    case "MovieSearchResults":
                        GUIContext.Instance.Client.GetSearchHints(
                            userId, item.Id, result => LoadSearchResultsAndContinue(result, e), ShowItemsErrorAndContinue);
                        return;
                    case "UserRootFolder":
                        LoadRootViews(e);
                        return;
                    case "View":
                        switch (item.Id)
                        {
                            case "root-music":
                                LoadMusicViewsAndContinue(e);
                                return;
                            case "root-movies":
                                LoadMovieViewsAndContinue(e);
                                return;
                            case "root-tvshows":
                                LoadTvShowsViewsAndContinue(e);
                                return;
                            case "movies-genres":
                                GUIContext.Instance.Client.GetGenres(GetItemsByNameQuery("Movie"),
                                    result => LoadItemsAndContinue(result, e), ShowItemsErrorAndContinue);
                                return;
                            case "movies-studios":
                                GUIContext.Instance.Client.GetStudios(GetItemsByNameQuery("Movie"),
                                    result => LoadItemsAndContinue(result, e), ShowItemsErrorAndContinue);
                                return;
                            case "movies-boxset":
                                query = query
                                    .BoxSets();
                                break;
                            case "movies-latest":
                                query = query
                                    .Movies()
                                    .SortBy(ItemSortBy.DateCreated)
                                    .Filters(ItemFilter.IsUnplayed)
                                    .Descending();
                                break;
                            case "movies-resume":
                                query = query
                                    .Movies()
                                    .SortBy(ItemSortBy.DatePlayed)
                                    .Filters(ItemFilter.IsResumable)
                                    .Descending();
                                break;
                            case "movies-all":
                                query = query
                                    .Movies()
                                    .SortBy(ItemSortBy.SortName);
                                break;
                            case "music-songs":
                                query = query
                                    .Audio()
                                    .SortBy(ItemSortBy.Album, ItemSortBy.SortName);
                                break;
                            case "tvshows-networks":
                                GUIContext.Instance.Client.GetStudios(GetItemsByNameQuery("Series"),
                                    result => LoadItemsAndContinue(result, e), ShowItemsErrorAndContinue);
                                return;
                            case "tvshows-all":
                                query = query
                                    .TvShows();
                                break;
                            case "tvshows-nextup":
                                var next = new NextUpQuery { UserId = userId, Limit = 24 };
                                GUIContext.Instance.Client.GetNextUp(next,
                                    result => LoadItemsAndContinue(result, e), ShowItemsErrorAndContinue);
                                return;
                            case "tvshows-latest":
                                query = query
                                    .Episode()
                                    .SortBy(ItemSortBy.DateCreated, ItemSortBy.SortName)
                                    .Filters(ItemFilter.IsUnplayed)
                                    .Descending();
                                break;
                            case "tvshows-genres":
                                GUIContext.Instance.Client.GetGenres(GetItemsByNameQuery("Series"),
                                    result => LoadItemsAndContinue(result, e), ShowItemsErrorAndContinue);
                                return;
                        }
                        break;
                    case "Genre":
                        query = query.Genres(item.Name).SortBy(ItemSortBy.SortName);
                        query = CurrentItem.Id == "tvshows-genres" ? query.TvShows() : query.Movies();
                        break;
                    case "Studio":
                        query = query.Studios(item.Name).SortBy(ItemSortBy.SortName);
                        query = CurrentItem.Id == "tvshows-networks" ? query.TvShows() : query.Movies();
                        break;
                    case "Series":
                        query = item.SeasonCount > 0 ? query.Season().ParentId(item.Id).SortBy(ItemSortBy.SortName) : query.ParentId(item.Id);
                        break;
                    case "Season":
                        query = query.Episode().ParentId(item.Id).SortBy(ItemSortBy.SortName);
                        break;
                    default:
                        // get movies by parent id
                        query = query.Movies().ParentId(item.Id).SortBy(ItemSortBy.SortName);
                        break;
                }

                // default is item query
                GUIContext.Instance.Client.GetItems(query.Apply(_sortableQuery),
                    result => LoadItemsAndContinue(result, e),
                    ShowItemsErrorAndContinue);
            });
        }

        private void LoadItemsAndContinue(ItemsResult result, ItemRequestEventArgs e)
        {
            foreach (var listitem in result.Items.Select(x => GetBaseListItem(x, e.Parent.TVTag as BaseItemDto)))
            {
                e.List.Add(listitem);
            }

            e.TotalItems = result.TotalRecordCount;
            _mre.Set();
        }

        private void LoadSearchResultsAndContinue(SearchHintResult result, ItemRequestEventArgs e)
        {
            foreach (var listitem in result.SearchHints.Select(x => GetBaseListItem(new BaseItemDto{Id = x.ItemId, Type = x.Type, Name = x.Name}, e.Parent.TVTag as BaseItemDto)))
            {
                e.List.Add(listitem);
            }

            e.TotalItems = result.TotalRecordCount;
            _mre.Set();
        }

        /// <summary>
        ///     Navigates the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        private void Navigate(GUIListItem item)
        {
            if (item == null || !(item.TVTag is BaseItemDto))
                return;

            var dto = item.TVTag as BaseItemDto;
            if (dto.Type.IsIn("Movie", "Episode", "Audio"))
            {
                ShowDetails(item);
            }
            else
            {
                _browser.Browse(item, null);
            }
        }

        /// <summary>
        ///     Loads the root folder into the navigation system.
        /// </summary>
        /// <param name="dto">The dto.</param>
        protected void LoadItem(BaseItemDto dto)
        {
            var listitem = GetBaseListItem(dto);
            Navigate(listitem);
        }

        protected void LoadRootViews(ItemRequestEventArgs request)
        {
            request.List.Add(GetViewListItem("root-movies", MediaBrowserPlugin.UI.Resource.Movies));
            request.List.Add(GetViewListItem("root-tvshows", MediaBrowserPlugin.UI.Resource.TVShows));
            request.List.Add(GetViewListItem("root-music", MediaBrowserPlugin.UI.Resource.Music));
            request.TotalItems = 3;

            _mre.Set();
        }

        /// <summary>
        ///     Loads the movie views and continues the main task.
        /// </summary>
        protected void LoadMovieViewsAndContinue(ItemRequestEventArgs request)
        {
            request.List.Add(GetViewListItem("movies-latest", MediaBrowserPlugin.UI.Resource.LatestUnwatchedMovies));
            request.List.Add(GetViewListItem("movies-resume", MediaBrowserPlugin.UI.Resource.ResumableMovies));
            request.List.Add(GetViewListItem("movies-all", MediaBrowserPlugin.UI.Resource.AllMovies));
            request.List.Add(GetViewListItem("movies-boxset", MediaBrowserPlugin.UI.Resource.BoxSets));
            request.List.Add(GetViewListItem("movies-genres", MediaBrowserPlugin.UI.Resource.Genres));
            request.List.Add(GetViewListItem("movies-studios", MediaBrowserPlugin.UI.Resource.Studios));
            request.TotalItems = 6;

            _mre.Set();
        }

        /// <summary>
        /// Loads the music views and continue.
        /// </summary>
        /// <param name="request">The <see cref="ItemRequestEventArgs"/> instance containing the event data.</param>
        protected void LoadMusicViewsAndContinue(ItemRequestEventArgs request)
        {
            request.List.Add(GetViewListItem("music-songs", MediaBrowserPlugin.UI.Resource.Songs));
            request.TotalItems = 1;
            _mre.Set();
        }

        /// <summary>
        ///     Loads the tv show views and continues the main task.
        /// </summary>
        protected void LoadTvShowsViewsAndContinue(ItemRequestEventArgs request)
        {
            request.List.Add(GetViewListItem("tvshows-latest", MediaBrowserPlugin.UI.Resource.LatestUnwatchedEpisodes));
            request.List.Add(GetViewListItem("tvshows-nextup", MediaBrowserPlugin.UI.Resource.NextUp));
            request.List.Add(GetViewListItem("tvshows-all", MediaBrowserPlugin.UI.Resource.Shows));
            request.List.Add(GetViewListItem("tvshows-genres", MediaBrowserPlugin.UI.Resource.Genres));
            request.List.Add(GetViewListItem("tvshows-networks", MediaBrowserPlugin.UI.Resource.Networks));
            request.TotalItems = 5;

            _mre.Set();
        }

        protected ItemsByNameQuery GetItemsByNameQuery(params string[] includeItemTypes)
        {
            var query = new ItemsByNameQuery
            {
                SortBy = new[] {ItemSortBy.SortName},
                SortOrder = SortOrder.Ascending,
                IncludeItemTypes = includeItemTypes,
                Recursive = true,
                Fields = new[] {ItemFields.DateCreated},
                UserId = GUIContext.Instance.Client.CurrentUserId
            };

            return query.Apply(_sortableQuery);
        }

        protected void ShowItemsErrorAndContinue(Exception e)
        {
            _mre.Set();
            ShowItemsError(e);
        }

        /// <summary>
        ///     Load item details window
        /// </summary>
        /// <param name="item">The item.</param>
        protected void ShowDetails(GUIListItem item)
        {
            var details = item.TVTag as BaseItemDto;
            details.IfNotNull(GUICommon.ViewDetails);
        }

        /// <summary>
        ///     Gets the unique identifier for the list item
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        protected string GetIdentifier(GUIListItem item)
        {
            return item == null ? string.Empty : item.Path;
        }

       

        /// <summary>
        ///     Publishes the artwork.
        /// </summary>
        /// <param name="item">The item.</param>
        protected override void PublishArtwork(BaseItemDto item)
        {
            var cover = string.Empty;

            SmartImageControl resource;
            if (ImageResources.TryGetValue(item.Type, out resource))
            {
                // load specific image
                cover = resource.GetImageUrl(item);
            } 
            else if (ImageResources.TryGetValue("Default", out resource))
            {
                cover = resource.GetImageUrl(item);
            }

            var backdrop = GetBackdropUrl(item);

            // todo: need a better way to do this 
            // the methods above are blocking (downloading and creating cache worst case and once they return 
            // the selection could be changed so we quickly check whether the image is still relevant
            if (!Facade.IsNull() && !Facade.SelectedListItem.IsNull() && Facade.SelectedListItem.TVTag == item)
            {
                _backdrop.Filename = backdrop ?? String.Empty;

                if (ImageResources.TryGetValue(item.Type, out resource))
                {
                    // load specific image
                    resource.Resource.Filename = cover;
                    return;
                }

                // load default image
                if (ImageResources.TryGetValue("Default", out resource))
                {
                    resource.Resource.Filename = cover;
                }
            }
        }

        protected void ShowFilterMenuDialog()
        {
            var result = GUIUtils.ShowMenuDialog(T.FilterOptions, _filters);
            if (result == -1) return;

            var filter = (ItemFilter)_filters[result].TVTag;
            if (!_sortableQuery.Filters.Remove(filter))
            {
                _sortableQuery.Filters.Add(filter);
            }

            _sortableQuery.Publish(MediaBrowserPlugin.DefaultProperty + ".Sortable");
            _browser.Reload(true);
        }

        protected void ShowSortMenuDialog()
        {
            var items = new List<GUIListItem>
            {
                GetSortItem(T.SortByName, ItemSortBy.SortName),
                GetSortItem(T.SortByBudget, ItemSortBy.Budget),
                GetSortItem(T.SortByCommunityRating, ItemSortBy.CommunityRating),
                GetSortItem(T.SortByContentRating, ItemSortBy.OfficialRating),
                GetSortItem(T.SortByCriticRating, ItemSortBy.CriticRating),
                GetSortItem(T.SortByDateAdded, ItemSortBy.DateCreated),
                GetSortItem(T.SortByDatePlayed, ItemSortBy.DatePlayed),
                GetSortItem(T.SortByDateReleased, ItemSortBy.PremiereDate),
                GetSortItem(T.SortByPlayCount, ItemSortBy.PlayCount),
                GetSortItem(T.SortByRevenue, ItemSortBy.Revenue),
                GetSortItem(T.SortByRuntime, ItemSortBy.Runtime),
            };

            var result = GUIUtils.ShowMenuDialog(T.SortOptions, items,
                items.FindIndex(x => x.Path == _sortableQuery.SortBy));
            if (result == -1) return;

            var field = items[result].Path;
            if (_sortableQuery.SortBy == field)
            {
                _sortableQuery.Descending = !_sortableQuery.Descending.GetValueOrDefault();
            }
            else
            {
                _sortableQuery.Descending = false;
            }

            _sortableQuery.SortBy = field;
            _sortableQuery.Publish(MediaBrowserPlugin.DefaultProperty + ".Sortable");

            CurrentItem = null;

            _browser.Reload(true);
        }

        protected void ShowSearchDialog()
        {
            var term = GUIUtils.ShowKeyboard("");
            if (term.IsNullOrWhiteSpace()) return;
            
            var dto = new BaseItemDto
            {
                Name = "Results for '" + term + "'",
                Type = "MovieSearchResults",
                Id = term.ToLower(),
                IsFolder = true
            };

            Navigate(GetBaseListItem(dto));
        }


        private static GUIListItem GetSortItem(string label, string field)
        {
            var item = new GUIListItem(label) {Path = field};
            return item;
        }

        private static GUIListItem GetFilterItem(ItemFilter filter)
        {
            var item = new GUIListItem(filter.ToString()) {TVTag = filter};
            return item;
        }
    }
}