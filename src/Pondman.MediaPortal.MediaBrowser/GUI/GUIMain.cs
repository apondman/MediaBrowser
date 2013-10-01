using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
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
    /// Main Browser
    /// </summary>
    public class GUIMain : GUIDefault
    {
        readonly GUIBrowser<string> _browser;
        readonly ManualResetEvent _mre;
        readonly List<GUIListItem> _filters;

        private SortableQuery _sortableQuery;

        public GUIMain()
            : base(MediaBrowserWindow.Main)
        {
            _browser = new GUIBrowser<string>(GetIdentifier, MediaBrowserPlugin.Log);
            _browser.Settings.Prefix = MediaBrowserPlugin.DefaultProperty;
            _browser.Settings.LoadingPlaceholderLabel = MediaBrowserPlugin.UI.Resource.LoadingMoreItems;
            _browser.ItemSelected += OnBaseItemSelected;
            _browser.ItemChanged += OnItemChanged;
            _browser.ItemsRequested += OnItemsRequested;

            _mre = new ManualResetEvent(false);
            _filters = new List<GUIListItem>();

            // register commands
            RegisterCommand("CycleLayout", CycleLayoutCommand);
            RegisterCommand("ChangeUser", ChangeUserCommand);
            RegisterCommand("Sort", SortCommand);
            RegisterCommand("Filter", FilterCommand);
            RegisterCommand("Search", SearchCommand);
        }

        #region Controls
        
        [SkinControl(50)]
        protected GUIFacadeControl Facade = null;

        #endregion

        #region Commands

        /// <summary>
        /// Cycles layout modes for the facade.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="actionType">Type of the action.</param>
        protected void CycleLayoutCommand(GUIControl control, MPGui.Action.ActionType actionType)
        {
            Facade.CycleLayout();
            Facade.Focus();
            Log.Debug("Layout: {0}", Facade.CurrentLayout);
        }

        /// <summary>
        /// Switch User
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="actionType">Type of the action.</param>
        protected void ChangeUserCommand(GUIControl control, MPGui.Action.ActionType actionType)
        {
            ShowUserProfilesDialog();
        }

        /// <summary>
        /// Sort the current view
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="actionType">Type of the action.</param>
        protected void SortCommand(GUIControl control, MPGui.Action.ActionType actionType)
        {
            ShowSortMenuDialog();
        }

        /// <summary>
        /// Shows the filter menu dialog.
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

            // update browser settings
            _browser.Settings.Limit = MediaBrowserPlugin.Settings.DefaultItemLimit;
            _browser.Attach(Facade);

            if (!GUIContext.Instance.IsServerReady) 
            {
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            if (!GUIContext.Instance.Client.IsUserLoggedIn)
            {
                //show dialog
                ShowUserProfilesDialog();
                return;
            }

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
                    GUIContext.Instance.Client.GetItem(Parameters.Id, GUIContext.Instance.Client.CurrentUserId, LoadItem, ShowItemsError);
                }
                return;
            }

            _browser.Reload();  
        }

        protected override void OnClicked(int controlId, GUIControl control, MPGui.Action.ActionType actionType)
        {
            if (_browser.IsBusy)
            {
                return;
            }

            switch (controlId)
            {
                // Facade
                case 50:
                    switch (actionType)
                    {
                        case MPGui.Action.ActionType.ACTION_SELECT_ITEM:
                            
                            // reset sortable query
                            // todo: bad place
                            _sortableQuery = new SortableQuery();

                            Navigate(Facade.SelectedListItem);
                            return;
                    }
                    break;
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
        /// Navigate back
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
        /// Gets an image for users
        /// </summary>
        /// <param name="item">The item.</param>
        public static void GetUserImage(GUIListItem item)
        {
            GUICommon.UserImageDownloadAndAssign.BeginInvoke(item, GUICommon.UserImageDownloadAndAssign.EndInvoke, null);
        }

        /// <summary>
        /// Gets an image for the item
        /// </summary>
        /// <param name="item">The item.</param>
        public static void GetItemImage(GUIListItem item)
        {
            GUICommon.ItemImageDownloadAndAssign.BeginInvoke(item, GUICommon.ItemImageDownloadAndAssign.EndInvoke, null);
        }

        /// <summary>
        /// Handler for selected item
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
        /// Handler for current item
        /// </summary>
        /// <param name="item">The item.</param>
        protected void OnItemChanged(GUIListItem item)
        {
            if (item == null) return;
            
            CurrentItem = item.TVTag as BaseItemDto;
            CurrentItem.IfNotNull(x => PublishItemDetails(x, MediaBrowserPlugin.DefaultProperty + ".Current"));
        }

        #endregion

        #region GUIListItem Factory Methods

        /// <summary>
        /// Gets the view list item.
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
        /// Gets the base list item.
        /// </summary>
        /// <param name="dto">The dto.</param>
        /// <returns></returns>
        public GUIListItem GetBaseListItem(BaseItemDto dto)
        {
            var item = new GUIListItem(dto.Name)
            {
                Path = dto.Type + "/" + dto.Id,
                Year = dto.ProductionYear.GetValueOrDefault(),
                TVTag = dto,
                IsFolder = dto.IsFolder,
                IconImage = "defaultVideo.png",
                IconImageBig = "defaultVideoBig.png",
                RetrieveArt = true,
                IsPlayed = dto.UserData != null ? dto.UserData.Played : false,
            };
            item.OnRetrieveArt += GetItemImage;
            
            switch(dto.Type) 
            {
                case "Episode":
                    item.Label = dto.IndexNumber.HasValue ? dto.IndexNumber.Value.ToString("0: " + item.Label) : string.Empty;
                    item.Label2 = dto.PremiereDate.HasValue ? dto.PremiereDate.Value.ToString(GUIUtils.Culture.DateTimeFormat.ShortDatePattern) : string.Empty;
                    break;
                case "Serie":
                case "Movie":
                    item.Label2 = dto.ProductionYear.HasValue ? dto.ProductionYear.ToString() : string.Empty;
                    break;
                case "Studio":
                case "Genre":
                    item.Label2 = dto.ChildCount.HasValue ? dto.ChildCount.ToString() : string.Empty;
                    break;
            }
            return item;
        }

        /// <summary>
        /// Gets the user list item.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns></returns>
        public GUIListItem GetUserListItem(UserDto user)
        {
            var item = new GUIListItem(user.Name)
            {
                Path = "User/" + user.Id,
                Label2 = 
                    user.LastLoginDate.HasValue
                        ? String.Format("{0}: {1}", MediaBrowserPlugin.UI.Resource.LastSeen, user.LastLoginDate.Value.ToShortDateString())
                        : string.Empty,
                TVTag = user,
                IconImage = "defaultPicture.png",
                IconImageBig = "defaultPictureBig.png",
                RetrieveArt = true
            };
            item.OnRetrieveArt += GetUserImage;

            return item;
        }

        #endregion

        /// <summary>
        /// Gets or sets the current item.
        /// </summary>
        /// <value>
        /// The current item.
        /// </value>
        public BaseItemDto CurrentItem { get; set; }

        /// <summary>
        /// Resets the  navigation to the starting position
        /// </summary>
        public void Reset()
        {
            GUIContext.Instance.PublishUser();

            _browser.Reset();
            CurrentItem = null;
            _sortableQuery = new SortableQuery();

            // get root folder
            GUIContext.Instance.Client.GetRootFolder(GUIContext.Instance.Client.CurrentUserId, LoadItem, ShowItemsError);
        }

        private void OnItemsRequested(object sender, ItemRequestEventArgs e)
        {
            Log.Debug("ItemsRequested()");

            WaitFor(x =>
            {
                var item = e.Parent.TVTag as BaseItemDto;
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
                        query = query.Movies().Search(item.Id);
                        break;
                    case "UserRootFolder":
                        LoadRootViews(e);
                        return;
                    case "View":
                        switch (item.Id)
                        {
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
                                    .Movies();
                                break;
                            case "tvshows-networks":
                                GUIContext.Instance.Client.GetStudios(GetItemsByNameQuery("Series"),
                                    result => LoadItemsAndContinue(result, e), ShowItemsErrorAndContinue);
                                return;
                            case "tvshows-all":
                                query = query
                                    .TvShows();
                                break;
                            case "tvshows-latest":
                                query = query
                                   .Episode()
                                   .SortBy(ItemSortBy.DateCreated)
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
                        query = query.Genres(item.Name);
                        if (CurrentItem.Id == "tvshows-genres")
                        {
                            query = query.TvShows();
                        }
                        else
                        {
                            query = query.Movies();
                        }
                        break;
                    case "Studio":
                        query = query.Studios(item.Name);
                        if (CurrentItem.Id == "tvshows-networks")
                        {
                            query = query.TvShows();
                        }
                        else
                        {
                            query = query.Movies();
                        }
                        break;
                    case "Series":
                        query = query.Season().ParentId(item.Id);
                        break;
                    case "Season":
                        query = query.Episode().ParentId(item.Id);
                        break;
                    default:
                        // get movies by parent id
                        query = query.Movies().ParentId(item.Id);
                        break;
                }

                // default is item query
                GUIContext.Instance.Client.GetItems(query.Apply(_sortableQuery), result => LoadItemsAndContinue(result, e),
                    ShowItemsErrorAndContinue);

            });
        }

        void LoadItemsAndContinue(ItemsResult result, ItemRequestEventArgs e)
        {
            foreach (var listitem in result.Items.Select(GetBaseListItem))
            {
                e.List.Add(listitem);
            }

            e.TotalItems = result.TotalRecordCount;

            if (e.Offset == 0)
            {
                string type = result.Items.Select(x => x.Type).FirstOrDefault();

                _filters.Clear();
                _filters.Add(GetFilterItem(ItemFilter.IsFavorite));
                _filters.Add(GetFilterItem(ItemFilter.Likes));
                _filters.Add(GetFilterItem(ItemFilter.Dislikes));
                
                if (type.IsIn("Movie", "Episode")) 
                {
                    _filters.Add(GetFilterItem(ItemFilter.IsPlayed));
                    _filters.Add(GetFilterItem(ItemFilter.IsUnplayed));
                    _filters.Add(GetFilterItem(ItemFilter.IsResumable));
                }
            }

            _mre.Set();
        }

        /// <summary>
        /// Navigates the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        private void Navigate(GUIListItem item)
        {
            if (item == null || !(item.TVTag is BaseItemDto))
                return;

            var dto = item.TVTag as BaseItemDto;
            if (dto.Type.IsIn("Movie", "Episode"))
            {
                ShowDetails(item);
            }
            else
            {
                _browser.Browse(item, null);
            }
        }

        /// <summary>
        /// Loads the root folder into the navigation system.
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
            request.TotalItems = 2;

            _mre.Set(); 
        }

        /// <summary>
        /// Loads the movie views and continues the main task.
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
        /// Loads the tv show views and continues the main task.
        /// </summary>
        protected void LoadTvShowsViewsAndContinue(ItemRequestEventArgs request)
        {
            request.List.Add(GetViewListItem("tvshows-latest", MediaBrowserPlugin.UI.Resource.LatestUnwatchedEpisodes));
            request.List.Add(GetViewListItem("tvshows-next", MediaBrowserPlugin.UI.Resource.NextUp));
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
                                SortBy = new [] { ItemSortBy.SortName },
                                SortOrder=SortOrder.Ascending,
                                IncludeItemTypes = includeItemTypes,
                                Recursive = true,
                                Fields = new [] { ItemFields.DateCreated},
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
        /// Load item details window
        /// </summary>
        /// <param name="item">The item.</param>
        protected void ShowDetails(GUIListItem item)
        {
            var details = item.TVTag as BaseItemDto;
            details.IfNotNull(GUICommon.ViewMovieDetails);
        }

        /// <summary>
        /// Gets the unique identifier for the list item
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        protected string GetIdentifier(GUIListItem item)
        {
            return item == null ? string.Empty : item.Path;
        }

        /// <summary>
        /// Shows the profile selection dialog.
        /// </summary>
        /// <param name="items">The items.</param>
        protected void ShowUserProfilesDialog(List<GUIListItem> items = null)
        {
            if (IsMainTaskRunning)
            {
                return;
            }
            
            if (items == null)
            {
                MainTask = GUITask.Run(LoadUserProfiles, ShowUserProfilesDialog, MediaBrowserPlugin.Log.Error, true);
                return;
            }

            var result = GUIUtils.ShowMenuDialog(MediaBrowserPlugin.UI.Resource.UserProfileLogin, items);
            if (result > -1)
            {
                var item = items[result];
                var user = item.TVTag as UserDto;

                var password = user != null && user.HasPassword ? GUIUtils.ShowKeyboard(string.Empty, true) : string.Empty;

                GUIContext.Instance.Client.AuthenticateUser(user.Id, password, success =>
                {
                    if (success)
                    {
                        GUIContext.Instance.Client.CurrentUser = user;
                        Reset();
                        return;
                    }
                    GUIUtils.ShowOKDialog(MediaBrowserPlugin.UI.Resource.UserProfileLogin, MediaBrowserPlugin.UI.Resource.UserProfileLoginFailed);
                    ShowUserProfilesDialog(items);
                });
            }
            else if (!GUIContext.Instance.Client.IsUserLoggedIn)
            {
                GUIWindowManager.ShowPreviousWindow();
            }
        }

        /// <summary>
        /// Loads the user profiles from the server
        /// </summary>
        /// <param name="task">The task.</param>
        /// <returns></returns>
        protected List<GUIListItem> LoadUserProfiles(GUITask task = null)
        {
            task = task ?? GUITask.None;
            var list = new List<GUIListItem>();

            WaitFor(x => GUIContext.Instance.Client.GetUsers(users =>
            {
                list.AddRange(users.TakeWhile(user => !task.IsCancelled).Select(GetUserListItem));
                x.Set();
            }, e =>
            {
                // todo: show error?
                Log.Error(e);
                x.Set();
            })
            ); // todo: timeout?

            return list;
        }

        /// <summary>
        /// Publishes the artwork.
        /// </summary>
        /// <param name="item">The item.</param>
        protected override void PublishArtwork(BaseItemDto item)
        {
            var cover = GetCoverUrl(item);
            var backdrop = GetBackdropUrl(item);

            // todo: need a better way to do this 
            // the methods above are blocking (downloading and creating cache worst case and once they return 
            // the selection could be changed so we quickly check whether the image is still relevant
            if (!Facade.IsNull() && !Facade.SelectedListItem.IsNull() && Facade.SelectedListItem.TVTag == item)
            {
               _cover.Filename = cover ?? String.Empty;
               _backdrop.Filename = backdrop ?? String.Empty;
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
            CurrentItem = null;
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

            var result = GUIUtils.ShowMenuDialog(T.SortOptions, items, items.FindIndex(x => x.Path == _sortableQuery.SortBy));
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
            string term = GUIUtils.ShowKeyboard("Enter Search Term");
            if (!term.IsNullOrWhiteSpace())
            {
                var dto = new BaseItemDto { Name = "Results: " + term, Id = term.ToLower(), Type = "MovieSearchResults", IsFolder = true };
                Navigate(GetBaseListItem(dto));
            }
        }

        /// <summary>
        /// Execute the given action and blocks using the ManualResetEvent.
        /// </summary>
        /// <param name="action">The action.</param>
        protected void WaitFor(Action<ManualResetEvent> action)
        {
            _mre.Reset();
            action(_mre);
            _mre.WaitOne();
        }

        static GUIListItem GetSortItem(string label, string field)
        {
            var item = new GUIListItem(label) {Path = field};
            return item;
        }

        static GUIListItem GetFilterItem(ItemFilter filter)
        {
            var item = new GUIListItem(filter.ToString()) { TVTag = filter };
            return item;
        }

    }
}
