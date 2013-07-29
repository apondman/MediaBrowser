using System.Linq;
using System.Windows.Controls;
using ConsoleApplication2.com.amazon.webservices;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MediaPortal.GUI.Library;
using Pondman.MediaPortal.GUI;
using System;
using System.Collections.Generic;
using System.Threading;
using MPGui = MediaPortal.GUI.Library;
using Pondman.MediaPortal.MediaBrowser.Models;
using MediaPortal.Dialogs;

namespace Pondman.MediaPortal.MediaBrowser.GUI
{

    /// <summary>
    /// Main Browser
    /// </summary>
    public class GUIMain : GUIDefault
    {
        readonly Stack<GUIListItem> _history;
        readonly GUIBrowser<string> _browser;
        readonly ManualResetEvent _mre;
        private SortableQuery _sortableQuery;

        public GUIMain()
            : base(MediaBrowserWindow.Main)
        {
            _history = new Stack<GUIListItem>();
            
            _browser = new GUIBrowser<string>(GetIdentifier);
            _browser.Settings.Prefix = MediaBrowserPlugin.DefaultProperty;
            _browser.ItemSelected += OnBaseItemSelected;
            _browser.CurrentItemChanged += OnCurrentItemChanged;
            _browser.PreloadRequested += OnBrowserPreloadRequested;

            _mre = new ManualResetEvent(false);

            // register commands
            RegisterCommand("CycleLayout", CycleLayoutCommand);
            RegisterCommand("ChangeUser", ChangeUserCommand);
            RegisterCommand("Sort", SortCommand);
        }

        #region Controls
        
        [SkinControl(50)]
        protected GUIFacadeControl Facade = null;

        #endregion

        #region Commands

        protected void CycleLayoutCommand(GUIControl control, MPGui.Action.ActionType actionType)
        {
            Facade.CycleLayout();
            Facade.Focus();
            Log.Debug("Layout: {0}", Facade.CurrentLayout);
        }

        protected void ChangeUserCommand(GUIControl control, MPGui.Action.ActionType actionType)
        {
            ShowUserProfilesDialog();
        }

        protected void SortCommand(GUIControl control, MPGui.Action.ActionType actionType)
        {
            // show sort dialog and set sort
        }

        #endregion 

        #region Window overrides

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            // update browser settings
            _browser.Limit = MediaBrowserPlugin.Settings.DefaultItemLimit;
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
                // todo: check type? add loading indicator?
                GUIContext.Instance.Client.GetItem(Parameters.Id, GUIContext.Instance.Client.CurrentUserId, LoadItem, ShowItemsError);
                return;
            }

            Reload();  
        }

        protected override void OnClicked(int controlId, GUIControl control, MPGui.Action.ActionType actionType)
        {
            if (IsMainTaskRunning)
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
            // todo: add contextual options
            // ShowUserProfilesDialog();
            ShowSortMenuDialog();
        }

        /// <summary>
        /// Navigate back
        /// </summary>
        protected override void OnPreviousWindow()
        {
            if (IsMainTaskRunning)
            {
                return;
            }

            // if we are in the root go to the previous window
            if (_history.Count == 1)
            {
                base.OnPreviousWindow();
                return;
            }

            // set the current item as the selected item
            SelectedId = GetIdentifier(_history.Pop());

            // navigate to the previous item
            Navigate(_history.Pop());
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
            if (item != null)
            {
                // todo: check for specific dto
                var dto = item.TVTag as BaseItemDto;
                PublishItemDetails(dto, MediaBrowserPlugin.DefaultProperty + ".Selected");
            }
        }

        /// <summary>
        /// Handler for current item
        /// </summary>
        /// <param name="item">The item.</param>
        protected void OnCurrentItemChanged(GUIListItem item)
        {
            if (item == null) return;
            CurrentItem = item.TVTag as BaseItemDto;

            if (CurrentItem != null)
            {
                PublishItemDetails(CurrentItem, MediaBrowserPlugin.DefaultProperty + ".Current");
            }
        }

        #endregion

        #region GUIListItem Factory Methods

        /// <summary>
        /// Gets the view list item.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="label">The label.</param>
        /// <returns></returns>
        public GUIListItem GetViewListItem(string id, string label)
        {
            var view = new BaseItemDto {Name = label, Id = id, Type = "View", IsFolder = true};

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
                Year = dto.ProductionYear ?? 0,
                TVTag = dto,
                IsFolder = dto.IsFolder,
                IconImage = "defaultVideo.png",
                IconImageBig = "defaultVideoBig.png",
                RetrieveArt = true
            };
            item.OnRetrieveArt += GetItemImage;

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
        /// Gets or sets the stored selection.
        /// </summary>
        /// <value>
        /// The selected id.
        /// </value>
        public string SelectedId { get; set; }

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

            CurrentItem = null;
            _history.Clear();
            SelectedId = null;
            _sortableQuery = new SortableQuery();

            // get root folder
            GUIContext.Instance.Client.GetRootFolder(GUIContext.Instance.Client.CurrentUserId, LoadItem, ShowItemsError);
        }

        /// <summary>
        /// Reloads the current browser state
        /// </summary>
        public void Reload()
        {
            var current = (_history.Count > 0 ? _history.Peek() : null);
            if (current != null)
            {
                if (!current.IsFolder)
                {
                    // if the item is not a folder act like we are going back a screen.
                    OnPreviousWindow();
                    return;
                }

                current = _history.Pop();
            }

            // otherwise reload the current state
            Navigate(current);
        }

        /// <summary>
        /// Navigates the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Navigate(GUIListItem item = null)
        {
            // push item into history
            _history.Push(item);

            // start
            if (item == null || !item.Path.StartsWith("Movie"))
            {
                MainTask = GUITask.Run(LoadItems, Publish, ShowItemsError);
            }
            else
            {
                ShowDetails(item);
            }
        }

        protected void OnBrowserPreloadRequested(object sender, EventArgs e)
        {
            if (IsMainTaskRunning) return;
            _browser.IsPreloading = true; // todo: find another way
            _sortableQuery.Offset = _browser.Count;

            MainTask = GUITask.Run(ContinueItems, Continue, ShowItemsError);   
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

        /// <summary>
        /// Loads en creates GUIListItems using the current item
        /// </summary>
        /// <param name="task">The task.</param>
        /// <returns></returns>
        protected List<GUIListItem> LoadItems(GUITask task)
        {
            // check the current item
            var current = _history.Peek();

            // do nothing if the data is already in the browser
            if (CurrentItem == (current.TVTag as BaseItemDto))
            {
                return null;
            }

            // clear browser and set current
            _browser.Current(current);

            // we are using the manual reset event because the call on the client is async and we want this method to only complete when it's done.
            _mre.Reset();
            GetItems(current.TVTag as BaseItemDto);
            _mre.WaitOne();

            return null;
        }

        protected List<GUIListItem> ContinueItems(GUITask task)
        {
            Log.Debug("Continue item request.");
            // we are using the manual reset event because the call on the client is async and we want this method to only complete when it's done.
            _mre.Reset();
            GetItems(CurrentItem);
            _mre.WaitOne();

            return null;
        }

        /// <summary>
        /// Gets the items. 
        /// </summary>
        /// <param name="item">The item.</param>
        protected void GetItems(BaseItemDto item)
        {
            // todo: this is a mess, rethink
            
            var userId = GUIContext.Instance.Client.CurrentUserId;
            var query = MediaBrowserQueries.Item
                                        .UserId(userId)
                                        .Recursive()
                                        .Fields(ItemFields.Overview, ItemFields.People, ItemFields.Genres, ItemFields.MediaStreams);
            if (_browser.Limit > 0)
            {
                _sortableQuery.Limit = 50;
            }

            Log.Debug("GetItems: Type={0}, Id={1}", item.Type, item.Id);

            switch (item.Type)
            {
                case "UserRootFolder":
                    LoadMovieViewsAndContinue();
                    return;
                case "View": 
                    switch (item.Id)
                    {
                        case "movies-genres":
                            GUIContext.Instance.Client.GetGenres(GetItemsByNameQueryForMovie(), PopulateBrowserAndContinue, ShowItemsErrorAndContinue);
                            return;
                        case "movies-studios":
                            GUIContext.Instance.Client.GetStudios(GetItemsByNameQueryForMovie(), PopulateBrowserAndContinue, ShowItemsErrorAndContinue);
                            return;
                        case "movies-boxset":
                            query = query
                                .BoxSets()
                                .Fields(ItemFields.ItemCounts);
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
                        case "tvshows":
                            query = query
                                .TvShows();
                            break;
                    }
                    break;
                case "Genre":
                    query = query.Movies().Genres(item.Name);
                    break;
                case "Studio":
                    query = query.Movies().Studios(item.Name);
                    break;
                default:
                    // get movies by parent id
                    query = query.Movies().ParentId(item.Id);
                    break;
            }

            // default is item query
            GUIContext.Instance.Client.GetItems(query.Apply(_sortableQuery), PopulateBrowserAndContinue, ShowItemsErrorAndContinue);
        }

        /// <summary>
        /// Loads the movie views and continues the main task.
        /// </summary>
        protected void LoadMovieViewsAndContinue()
        {
            _browser.Add(GetViewListItem("movies-latest", MediaBrowserPlugin.UI.Resource.LatestUnwatchedMovies));
            _browser.Add(GetViewListItem("movies-resume", MediaBrowserPlugin.UI.Resource.ResumableMovies));
            _browser.Add(GetViewListItem("movies-all", MediaBrowserPlugin.UI.Resource.AllMovies));
            _browser.Add(GetViewListItem("movies-boxset", MediaBrowserPlugin.UI.Resource.BoxSets));
            _browser.Add(GetViewListItem("movies-genres", MediaBrowserPlugin.UI.Resource.Genres));
            _browser.Add(GetViewListItem("movies-studios", MediaBrowserPlugin.UI.Resource.Studios));

            _browser.TotalCount = 5;
            _mre.Set();
        }

        protected ItemsByNameQuery GetItemsByNameQueryForMovie(int startIndex = 0)
        {
            var query = new ItemsByNameQuery
                            {
                                SortBy = new [] { ItemSortBy.SortName },
                                SortOrder=SortOrder.Ascending,
                                IncludeItemTypes = new [] {"Movie"},
                                Recursive = true,
                                Fields = new [] {ItemFields.ItemCounts, ItemFields.DateCreated},
                                UserId = GUIContext.Instance.Client.CurrentUserId
                            };

            return query.Apply(_sortableQuery);
        }

        protected void PopulateBrowserAndContinue(ItemsResult result)
        {
            _browser.TotalCount = result.TotalRecordCount;
            
            foreach (var item in result.Items)
            {
                if (MainTask.IsCancelled)
                {
                    break;
                }

                var listitem = GetBaseListItem(item);
                _browser.Add(listitem);
            }

            _mre.Set();
        }

        protected void ShowItemsErrorAndContinue(Exception e)
        {
            _browser.IsPreloading = false;
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
        /// Publishes the specified items.
        /// </summary>
        /// <param name="items">The items.</param>
        protected void Publish(List<GUIListItem> items)
        {
            _browser.Publish(SelectedId);
        }

        protected void Continue(List<GUIListItem> items)
        {
            _browser.Continue();
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
            
            // we are using the manual reset event because the call on the client is async and we want this method to only complete when it's done.
            _mre.Reset();
            var list = new List<GUIListItem>();

            GUIContext.Instance.Client.GetUsers(users =>
            {
                list.AddRange(users.TakeWhile(user => !task.IsCancelled).Select(GetUserListItem));

                _mre.Set();
            }
                , e => {
                    // todo: show error?
                    Log.Error(e);
                    _mre.Set();
                });

            _mre.WaitOne(); // todo: timeout?

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
            if (Facade != null && Facade.SelectedListItem != null && Facade.SelectedListItem.TVTag == item)
            {
               _cover.Filename = cover;
               _backdrop.Filename = backdrop;
            }            
        }

        protected void ShowSortMenuDialog()
        {
            var items = new List<GUIListItem>
            {
                GetSortItem(ItemSortBy.SortName, ItemSortBy.SortName),
                GetSortItem(ItemSortBy.PremiereDate, ItemSortBy.PremiereDate),
                GetSortItem(ItemSortBy.DateCreated, ItemSortBy.DateCreated),
                GetSortItem(ItemSortBy.Runtime, ItemSortBy.Runtime)
            };

            var result = GUIUtils.ShowMenuDialog("Sort options", items, items.FindIndex(x => x.Path == _sortableQuery.SortBy));
            if (result == -1) return;
            
            var field = items[result].Path;
            if (_sortableQuery.SortBy == field)
            {
                _sortableQuery.Descending = !_sortableQuery.Descending;
            }
            else
            {
                _sortableQuery.Descending = false;
            }

            _sortableQuery.SortBy = field;
            _sortableQuery.Offset = 0;

            _sortableQuery.Publish(MediaBrowserPlugin.DefaultProperty + ".Sortable");
            CurrentItem = null;
            Reload();
        }

        static GUIListItem GetSortItem(string label, string field)
        {
            var item = new GUIListItem(label) {Path = field};
            return item;
        }
    }
}
