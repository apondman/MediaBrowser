using System.Runtime.Remoting.Channels;
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
using MPGui = MediaPortal.GUI.Library;
using System.Threading.Tasks;
using System.Threading;
using MediaBrowser.Model.Channels;

namespace Pondman.MediaPortal.MediaBrowser.GUI
{
    /// <summary>
    ///     Main Browser
    /// </summary>
    public class GUIMain : GUIDefault
    {
        private readonly GUIBrowser _browser;
        private readonly Dictionary<string, GUIFacadeControl> _facades;
        private SortableQuery _sortableQuery;

        public GUIMain()
            : base(MediaBrowserWindow.Main)
        {
            _browser = new GUIBrowser(MediaBrowserPlugin.Log);
            _browser.Settings.Prefix = MediaBrowserPlugin.DefaultProperty;
            _browser.Settings.LoadingPlaceholderLabel = MediaBrowserPlugin.UI.Resource.LoadingMoreItems;
            _browser.ItemSelected += OnBaseItemSelected;
            _browser.ItemPublished += OnItemPublished;
            _browser.ItemChanged += OnItemChanged;
            _browser.ItemsRequested += OnItemsRequested;
            _browser.LoadingStatusChanged += OnLoadingStatusChanged;

            _facades = new Dictionary<string, GUIFacadeControl>();

            // register commands
            RegisterCommand("CycleLayout", CycleLayoutCommand);
            RegisterCommand("Sort", SortCommand);
            RegisterCommand("Filter", FilterCommand);
            RegisterCommand("Search", SearchCommand);
            RegisterCommand("StartsWith", StartsWithCommand);
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

        protected void StartsWithCommand(GUIControl control, MPGui.Action.ActionType actionType)
        {
            ShowAlphabetDialog();
        }

        #endregion

        #region Window overrides

        public override void DeInit()
        {
            MediaBrowserPlugin.Shutdown();
            base.DeInit();
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            // no facades!
            if (_facades.Count == 0)
            {
                // todo: dialog?
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            if (!GUIContext.Instance.IsServerReady || !GUIContext.Instance.Client.IsUserLoggedIn) return;
            OnWindowStart();
        }

        protected async void OnWindowStart()
        {
            // browse to item
            if (!String.IsNullOrEmpty(Parameters.Id))
            {
                _logger.Debug("OnWindowStart(): Type={0}, Id={1}", Parameters.Type, Parameters.Id);
                if (Parameters.Type == "View")
                {
                    var item = GetViewListItem(Parameters.Id);
                    Navigate(item);
                }
                else
                {
                    try
                    {
                        var item = await GUIContext.Instance.Client.GetItemAsync(Parameters.Id, GUIContext.Instance.Client.CurrentUserId);
                        LoadItem(item);
                    }
                    catch (Exception e)
                    {
                        ShowItemsError(e);
                    }
                }
                return;
            }

            // Reload state
            if (!_browser.Reload())
            {
                // get root folder if there's nothing to reload
                try
                {
                    var item = await GUIContext.Instance.Client.GetRootFolderAsync(GUIContext.Instance.Client.CurrentUserId);
                    LoadItem(item);
                }
                catch (Exception e)
                {
                    ShowItemsError(e);
                }
            }
        }

        protected override void OnWindowLoaded()
        {
            base.OnWindowLoaded();

            if (controlList != null)
                controlList
                    .OfType<GUIFacadeControl>()
                    .Where(x => x.Description.StartsWith("MediaBrowser.Facade."))
                    .Select(x => new { facade = x, vars = x.Description.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries) })
                    .ToList()
                    .ForEach(x => {

                        int tokens = x.vars.Length;
                        if (tokens > 2)
                        {
                            _facades[x.vars[2]] = x.facade; // type
                        }
                        if (tokens > 3)
                        {
                            _facades[x.vars[2] + "." + x.vars[3]] = x.facade; // type+id
                        }
                      }
                    );
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
                case MPGui.Action.ActionType.ACTION_PREVIOUS_MENU:
                    _browser.Cancel();

                    if (MediaBrowserPlugin.Config.Settings.UiUseUniversalBackButton)
                    {
                        OnPreviousWindow();
                    }
                    else
                    {
                        base.OnPreviousWindow();
                    }                 

                    break;
                case MPGui.Action.ActionType.ACTION_PARENT_DIR:
                    _browser.Cancel();
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
            // ShowSortMenuDialog();
            // ShowSearchDialog();
            ShowFilterMenuDialog();
        }

        /// <summary>
        ///     Navigate back
        /// </summary>
        protected override void OnPreviousWindow()
        {
            // reset sortable query
            _sortableQuery = new SortableQuery();
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
        protected async void OnBaseItemSelected(GUIListItem item)
        {
            if (item == null) return;

            // todo: check for specific dto
            var dto = item.TVTag as BaseItemDto;
            if (dto != null)
            {
                await PublishItemDetails(dto, MediaBrowserPlugin.DefaultProperty + ".Selected");
            }
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

            // update publish delay ms
            _browser.Settings.Delay = MediaBrowserPlugin.Config.Settings.PublishDelayMs;
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

        protected void OnLoadingStatusChanged(bool isLoading)
        {
            isLoading.Publish(".Loading");

            if (isLoading)
            {
                // todo: setting to disable wait cursor
                GUIWaitCursor.Init();
                GUIWaitCursor.Show();
                return;
            }

            GUIWaitCursor.Hide();
        }

        GUIFacadeControl GetTypedFacade(BaseItemDto dto)
        {
            GUIFacadeControl facade;
            if (!_facades.TryGetValue(dto.Type + "." + dto.Id, out facade))
            {
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
            var view = new BaseItemDto { Name = label ?? id, Id = id, Type = "View", IsFolder = true };

            return view.ToListItem();
        }

        #endregion

        /// <summary>
        ///     Gets or sets the current item.
        /// </summary>
        /// <value>
        ///     The current item.
        /// </value>
        public BaseItemDto CurrentItem
        {
            get { return _currentItem; }
            set { _currentItem = value; OnCurrentItemChanged(); }
        }private BaseItemDto _currentItem;

        private async void OnCurrentItemChanged()
        {
            if (_currentItem == null) return;

            GUIContext.Instance.Update(_currentItem); // todo: context?
            await PublishItemDetails(_currentItem, MediaBrowserPlugin.DefaultProperty + ".Current");
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

            OnWindowStart();
        }

        private void OnItemsRequested(object sender, ItemRequestEventArgs e)
        {
            var item = e.Parent.TVTag as BaseItemDto;
            if (item == null) return;

            Log.Debug("ItemsRequested()");

            // todo: this is a mess, rethink
            
            //GUIContext.Instance.Client.CurrentUser.Configuration.DisplayMissingEpisodes
            var client = GUIContext.Instance.Client;
            var userSettings = client.CurrentUser.Configuration;
            var userId = client.CurrentUserId;
            var query = MediaBrowserQueries.Item
                            .UserId(userId)
                            .Recursive()
                            .Fields(ItemFields.Overview, ItemFields.People, ItemFields.Genres, ItemFields.MediaSources, ItemFields.PrimaryImageAspectRatio);

            // Enforce user configuration
            if (!userSettings.DisplayMissingEpisodes)
            {
                query = query.Missing(false);
            }

            if (!userSettings.DisplayUnairedEpisodes)
            {
                query = query.Unaired(false);
            }

            if (_browser.Settings.Limit > 0)
            {
                _sortableQuery.Limit = _browser.Settings.Limit;
                _sortableQuery.Offset = e.Offset;
            }

            Log.Debug("GetItems: Type={0}, Id={1}", item.Type, item.Id);

            try
            {
                switch (item.Type)
                {
                    case "MovieSearchResults":
                        var hints = client.GetSearchHintsAsync(new SearchQuery { UserId = userId, SearchTerm = item.Id });
                        LoadItems(hints.Result, e);
                        return;
                    case MediaBrowserType.Channel:
                        LoadItems(client.GetChannelItems(new ChannelItemQuery { UserId = userId, ChannelId = item.Id }), e);
                        return;
                    case MediaBrowserType.ChannelFolderItem:
                        LoadItems(client.GetChannelItems(new ChannelItemQuery { UserId = userId, ChannelId = item.ChannelId, FolderId = item.Id }), e);
                        return;
                    case MediaBrowserType.UserRootFolder:
                        LoadItems(client.GetUserViews(userId), e);
                        return;
                    case MediaBrowserType.Folder:
                    case MediaBrowserType.CollectionFolder:
                    case MediaBrowserType.UserView:
                        query = query.Recursive(false).ParentId(item.Id).SortBy(ItemSortBy.SortName);
                        break;
                    case MediaBrowserType.View:
                        switch (item.Id)
                        {
                            case "root-channels":
                                LoadItems(client.GetChannels(new ChannelQuery { UserId = userId }), e);
                                return;
                            case "root-mediafolders":
                                var rootId = client.GetRootFolderAsync(client.CurrentUserId).Result.Id;
                                query = query.Recursive(false).ParentId(rootId).SortBy(ItemSortBy.SortName);
                                break;
                            case "root-music":
                                LoadMusicViewsAndContinue(e);
                                return;
                            case "root-movies":
                                LoadMovieViewsAndContinue(e);
                                return;
                            case "root-tvshows":
                                LoadTvShowsViewsAndContinue(e);
                                return;
                            case "root-collections":
                                query = query.Collections();
                                break;
                            case "movies-genres":
                                LoadItems(client.GetGenresAsync(MediaBrowserQueries.Named.User(userId).Include(MediaBrowserType.Movie).Apply(_sortableQuery)), e);
                                return;
                            case "movies-studios":
                                LoadItems(client.GetStudiosAsync(MediaBrowserQueries.Named.User(userId).Include(MediaBrowserType.Movie).Apply(_sortableQuery)), e);
                                return;                           
                            case "movies-people":
                                LoadItems(client.GetPeopleAsync(MediaBrowserQueries.Persons.User(userId).Fields(ItemFields.Overview, ItemFields.PrimaryImageAspectRatio).Include(MediaBrowserType.Movie).Apply(_sortableQuery), CancellationToken.None), e);
                                return;
                            case "music-artists":
                                LoadItems(client.GetItemsByNameAsync("Artists",
                                    MediaBrowserQueries.Named.User(userId).Apply(_sortableQuery)), e);
                                return;
                            case "tvshows-networks":
                                LoadItems(client.GetStudiosAsync(
                                    MediaBrowserQueries.Named.User(userId).Include(MediaBrowserType.Series).Apply(_sortableQuery)), e);
                                return;
                            case "tvshows-genres":
                                LoadItems(client.GetGenresAsync(
                                    MediaBrowserQueries.Named.User(userId).Include(MediaBrowserType.Series).Apply(_sortableQuery)), e);
                                return;
                            case "tvshows-people":
                                LoadItems(client.GetPeopleAsync(
                                    MediaBrowserQueries.Persons.User(userId).Fields(ItemFields.Overview, ItemFields.PrimaryImageAspectRatio).Include(MediaBrowserType.Series).Apply(_sortableQuery), CancellationToken.None), e);
                                return;
                        }
                        break;
                    case MediaBrowserType.Artist:
                        query = query.Artists(item.Name)
                                .MusicAlbum().Audio()
                                .SortBy(ItemSortBy.ProductionYear, ItemSortBy.SortName);
                        break;
                    case MediaBrowserType.Genre:
                        query = query.Genres(item.Name).SortBy(ItemSortBy.SortName);
                        query = CurrentItem.Id.Contains("tvshows") ? query.Series() : query.Movies();
                        break;
                    case MediaBrowserType.Studio:
                        query = query.Studios(item.Name).SortBy(ItemSortBy.SortName);
                        query = CurrentItem.Id.Contains("tvshows") ? query.Series() : query.Movies();
                        break;
                    case MediaBrowserType.Series:
                        query = item.SeasonCount > 0 ? query.Season().ParentId(item.Id).SortBy(ItemSortBy.SortName) : query.ParentId(item.Id);
                        break;
                    case MediaBrowserType.Season:
                        LoadItems(client.GetEpisodesAsync(new EpisodeQuery { IsVirtualUnaired = userSettings.DisplayUnairedEpisodes, IsMissing = !userSettings.DisplayMissingEpisodes ? false : (bool?)null, SeasonId = item.Id, SeriesId = item.SeriesId, UserId = userId, Fields = new ItemFields[] { ItemFields.Overview, ItemFields.People, ItemFields.Genres, ItemFields.MediaStreams, ItemFields.PrimaryImageAspectRatio } }, CancellationToken.None), e);
                        return;
                    case MediaBrowserType.Person:
                        query.Person(item.Name).SortBy(ItemSortBy.SortName);
                        query = CurrentItem.Id.Contains("movies") ? query.Movies() : query.Series();
                        break;
                    case MediaBrowserType.BoxSet:
                        query = query.ParentId(item.Id).SortBy(ItemSortBy.ProductionYear, ItemSortBy.SortName);
                        break;
                    default:
                        // get by parent id
                        query = query.ParentId(item.Id).SortBy(ItemSortBy.SortName);

                        break;
                }

                // default is item query
                LoadItems(client.GetItemsAsync(query.Apply(_sortableQuery), CancellationToken.None), e);
            }
            catch (Exception ex)
            {
                ShowItemsError(ex);
            }
        }

        private void LoadItems(Task<QueryResult<BaseItemDto>> task, ItemRequestEventArgs args)
        {
            var result = task.Result;

            var items = result.Items.Select(x => x.ToListItem()).ToList();
            var total = result.TotalRecordCount;

            LoadItems(items, args, total);
        }

        private void LoadItems(Task<ItemsResult> task, ItemRequestEventArgs args)
        {
            LoadItems(task.Result, args);
        }

        private void LoadItems(ItemsResult result, ItemRequestEventArgs args)
        {
            var dto = args.Parent.TVTag as BaseItemDto;
            
            var items = result.Items.Select(x => x.ToListItem(dto)).ToList();
            var total = result.TotalRecordCount;
            
            /*
            if (dto.Type == MediaBrowserType.UserRootFolder && dto.Id != "root-mediafolders")
            {
                //items.Clear();
                //items.Add(GetViewListItem("root-collections", MediaBrowserPlugin.UI.Resource.Collections));
                //items.Add(GetViewListItem("root-movies", MediaBrowserPlugin.UI.Resource.Movies));
                //items.Add(GetViewListItem("root-tvshows", MediaBrowserPlugin.UI.Resource.TVShows));
                //items.Add(GetViewListItem("root-music", MediaBrowserPlugin.UI.Resource.Music));
                //items.Add(GetViewListItem("root-channels", MediaBrowserPlugin.UI.Resource.Channels));
                //items.Add(GetViewListItem("root-mediafolders", MediaBrowserPlugin.UI.Resource.MediaFolders));
                total = items.Count;
            }*/

            LoadItems(items, args, total);
        }

        private void LoadItems(SearchHintResult result, ItemRequestEventArgs args)
        {
            LoadItems(result.SearchHints.Select(x => new BaseItemDto { Id = x.ItemId, Type = x.Type, Name = x.Name }.ToListItem(args.Parent.TVTag as BaseItemDto)), args, result.TotalRecordCount);
        }

        private void LoadItems(IEnumerable<GUIListItem> items, ItemRequestEventArgs args, int total)
        {
            foreach (var item in items)
            {
                args.List.Add(item);
            }

            args.TotalItems = total;
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
            if (dto.Type.IsIn(MediaBrowserType.Movie, MediaBrowserType.Episode, MediaBrowserType.Audio, MediaBrowserType.ChannelVideoItem))
            {
                ShowDetails(item);
            }
            else
            {
                _browser.Settings.Limit = MediaBrowserPlugin.Config.Settings.DefaultItemLimit;
                _browser.Browse(item, -1);
            }
        }

        /// <summary>
        ///     Loads the root folder into the navigation system.
        /// </summary>
        /// <param name="dto">The dto.</param>
        protected void LoadItem(BaseItemDto dto)
        {
            var listitem = dto.ToListItem();
            Navigate(listitem);
        }

        /// <summary>
        ///     Loads the movie views and continues the main task.
        /// </summary>
        protected void LoadMovieViewsAndContinue(ItemRequestEventArgs request)
        {
            request.List.Add(GetViewListItem("movies-latest", MediaBrowserPlugin.UI.Resource.RecentlyAddedMovies));
            request.List.Add(GetViewListItem("movies-unwatched", MediaBrowserPlugin.UI.Resource.LatestUnwatchedMovies));
            request.List.Add(GetViewListItem("movies-resume", MediaBrowserPlugin.UI.Resource.ResumableMovies));
            request.List.Add(GetViewListItem("movies-all", MediaBrowserPlugin.UI.Resource.AllMovies));
            request.List.Add(GetViewListItem("movies-boxset", MediaBrowserPlugin.UI.Resource.Collections));
            request.List.Add(GetViewListItem("movies-genres", MediaBrowserPlugin.UI.Resource.Genres));
            request.List.Add(GetViewListItem("movies-studios", MediaBrowserPlugin.UI.Resource.Studios));
            request.List.Add(GetViewListItem("movies-people", MediaBrowserPlugin.UI.Resource.People));
            request.TotalItems = request.List.Count;
        }

        /// <summary>
        /// Loads the music views and continue.
        /// </summary>
        /// <param name="request">The <see cref="ItemRequestEventArgs"/> instance containing the event data.</param>
        protected void LoadMusicViewsAndContinue(ItemRequestEventArgs request)
        {
            request.List.Add(GetViewListItem("music-songs", MediaBrowserPlugin.UI.Resource.Songs));
            request.List.Add(GetViewListItem("music-albums", MediaBrowserPlugin.UI.Resource.Albums));
            //request.List.Add(GetViewListItem("music-genres", MediaBrowserPlugin.UI.Resource.Genres));
            request.List.Add(GetViewListItem("music-artists", MediaBrowserPlugin.UI.Resource.Artists));
            request.TotalItems = request.List.Count;
        }

        /// <summary>
        ///     Loads the tv show views and continues the main task.
        /// </summary>
        protected void LoadTvShowsViewsAndContinue(ItemRequestEventArgs request)
        {
            request.List.Add(GetViewListItem("tvshows-nextup", MediaBrowserPlugin.UI.Resource.NextUp));
            request.List.Add(GetViewListItem("tvshows-latest", MediaBrowserPlugin.UI.Resource.RecentlyAddedEpisodes));
            request.List.Add(GetViewListItem("tvshows-unwatched", MediaBrowserPlugin.UI.Resource.LatestUnwatchedEpisodes));
            request.List.Add(GetViewListItem("tvshows-all", MediaBrowserPlugin.UI.Resource.Shows));
            request.List.Add(GetViewListItem("tvshows-genres", MediaBrowserPlugin.UI.Resource.Genres));
            request.List.Add(GetViewListItem("tvshows-networks", MediaBrowserPlugin.UI.Resource.Networks));
            request.List.Add(GetViewListItem("tvshows-people", MediaBrowserPlugin.UI.Resource.People));
            request.TotalItems = request.List.Count;
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
        ///     Publishes the artwork.
        /// </summary>
        /// <param name="item">The item.</param>
        protected override async Task PublishArtwork(BaseItemDto item)
        {
            var backdrop = await GetBackdropUrl(item);

            // todo: need a better way to do this 
            // the methods above are blocking (downloading and creating cache worst case and once they return 
            // the selection could be changed so we quickly check whether the image is still relevant
            if (!Facade.IsNull() && !Facade.SelectedListItem.IsNull() && Facade.SelectedListItem.TVTag == item)
            {
                backdropHandler.Filename = backdrop ?? String.Empty;
                UpdateSmartImageControls(item);
            }
        }

        protected void ShowFilterMenuDialog()
        {
            // root and root views don't have filter options
            if (Facade.Count == 0 || _currentItem.Id.Contains("root-") || _currentItem.Type == "UserRootFolder") return;

            var filters = new List<GUIListItem>
            {
                GetFilterItem(ItemFilter.IsFavorite),
                GetFilterItem(ItemFilter.Likes),
                GetFilterItem(ItemFilter.Dislikes)
            };

            // todo: look at first item for now
            var item = Facade[0].TVTag as BaseItemDto;
            if (item == null) return;

            if (item.Type.IsIn(MediaBrowserType.Movie, MediaBrowserType.Episode, MediaBrowserType.Video, MediaBrowserType.Audio))
            {
                filters.Add(GetFilterItem(ItemFilter.IsPlayed));
                filters.Add(GetFilterItem(ItemFilter.IsUnplayed));
                filters.Add(GetFilterItem(ItemFilter.IsResumable));
            }

            if (item.Type == MediaBrowserType.Person)
            {
                filters.Add(GetSortItem(PersonType.Actor));
                filters.Add(GetSortItem(PersonType.Composer));
                filters.Add(GetSortItem(PersonType.Director));
                filters.Add(GetSortItem(PersonType.GuestStar));
                filters.Add(GetSortItem(PersonType.Producer));
                filters.Add(GetSortItem(PersonType.Writer));
            }

            var result = GUIUtils.ShowMenuDialog(T.FilterOptions, filters);
            if (result == -1) return;

            var filter = filters[result].TVTag;
            if (filter is ItemFilter)
            {
                if (!_sortableQuery.Filters.Remove((ItemFilter)filter))
                {
                    _sortableQuery.Filters.Add((ItemFilter)filter);
                }
            }
            else
            {
                if (!_sortableQuery.PersonTypes.Remove(filters[result].Path))
                {
                    _sortableQuery.PersonTypes.Add(filters[result].Path);
                }
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

            // todo: not needed
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

            Navigate(dto.ToListItem());
        }

        protected void ShowAlphabetDialog()
        {
            var list =
                "#ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray()
                    .Select(x => new GUIListItem { Label = x.ToString(), Path = x.ToString() });
            var items = new List<GUIListItem>(list);

            var result = GUIUtils.ShowMenuDialog(T.StartsWith, items, items.FindIndex(x => x.Path == _sortableQuery.StartsWith));
            if (result == -1) return;

            _sortableQuery.StartsWith = items[result].Path;
            _sortableQuery.Publish(MediaBrowserPlugin.DefaultProperty + ".Sortable");

            _browser.Reload(true);
        }

        private static GUIListItem GetSortItem(string label, string field = null)
        {
            var item = new GUIListItem(label) { Path = field ?? label };
            return item;
        }

        private static GUIListItem GetFilterItem(ItemFilter filter)
        {
            var item = new GUIListItem(filter.ToString()) { TVTag = filter };
            return item;
        }

    }
}