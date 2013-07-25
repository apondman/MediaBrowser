using System.Linq;
using System.Windows.Controls;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MediaPortal.GUI.Library;
using Pondman.MediaPortal.GUI;
using System;
using System.Collections.Generic;
using System.Threading;
using MPGui = MediaPortal.GUI.Library;

namespace Pondman.MediaPortal.MediaBrowser.GUI
{

    /// <summary>
    /// Main Browser
    /// </summary>
    public class GUIMain : GUIDefault
    {
        Stack<GUIListItem> _history;
        GUIBrowser<string> _browser;
        ManualResetEvent _mre;

        public GUIMain()
            : base(MediaBrowserWindow.Main)
        {
            _history = new Stack<GUIListItem>();
            
            _browser = new GUIBrowser<string>(GetIdentifier);
            _browser.Settings.Prefix = MediaBrowserPlugin.DefaultProperty;
            _browser.PublishSelected += OnBaseItemSelected;
            _browser.PublishCurrent += OnPublishCurrent;

            _mre = new ManualResetEvent(false);

            // register commands
            RegisterCommand("CycleLayout", CycleLayoutCommand);
            RegisterCommand("ChangeUser", ChangeUserCommand);
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

        #endregion 

        #region Window overrides

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

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
            ShowUserProfilesDialog();
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
        protected void OnPublishCurrent(GUIListItem item)
        {
            if (item != null)
            {
                CurrentItem = item.TVTag as BaseItemDto;
                if (CurrentItem != null)
                {
                    PublishItemDetails(CurrentItem, MediaBrowserPlugin.DefaultProperty + ".Current");
                }
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
                        ? "Last seen: " + user.LastLoginDate.Value.ToShortDateString()
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

            // clear browser and set current
            _browser.Current(current);

            // we are using the manual reset event because the call on the client is async and we want this method to only complete when it's done.
            _mre.Reset();
            GetItems(current.TVTag as BaseItemDto);
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
            var query = MediaBrowserQueries.New
                                        .UserId(userId)
                                        .Recursive()
                                        .Fields(ItemFields.Overview, ItemFields.People, ItemFields.Genres, ItemFields.MediaStreams);
            Log.Debug("GetItems: Type={0}, Id={1}", item.Type, item.Id);

            switch (item.Type)
            {
                case "UserRootFolder":
                    LoadViewsAndContinue();
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
                                        .Fields(ItemFields.ItemCounts)
                                        .SortBy(ItemSortBy.SortName)
                                        .Ascending();
                            break;
                        case "movies-latest":
                            query = query
                                    .Movies()
                                    .SortBy(ItemSortBy.DateCreated)
                                    .Filters(ItemFilter.IsUnplayed)
                                    .Descending()
                                    .Limit(100);
                            break;
                        case "movies-all":
                            query = query
                                    .Movies()
                                    .SortBy(ItemSortBy.SortName)
                                    .Ascending();
                            break;
                        case "tvshows":
                            query = query
                                    .TVShows()
                                    .SortBy(ItemSortBy.SortName)
                                    .Ascending();
                            break;
                    }
                    break;
                case "Genre":
                    query = query.Movies().Genres(item.Name).SortBy(ItemSortBy.SortName).Ascending();
                    break;
                case "Studio":
                    query = query.Movies().Studios(item.Name).SortBy(ItemSortBy.SortName).Ascending();
                    break;
                default:
                    // get movies by parent id
                    query = query.Movies().ParentId(item.Id).SortBy(ItemSortBy.SortName).Ascending();
                    break;
            }

            // default is item query
            GUIContext.Instance.Client.GetItems(query, PopulateBrowserAndContinue, ShowItemsErrorAndContinue);
        }

        protected void LoadViewsAndContinue()
        {
            _browser.Add(GetViewListItem("movies-latest", MediaBrowserPlugin.UI.Resource.LatestUnwatchedMovies));
            _browser.Add(GetViewListItem("movies-all", MediaBrowserPlugin.UI.Resource.AllMovies));
            _browser.Add(GetViewListItem("movies-boxset", MediaBrowserPlugin.UI.Resource.BoxSets));
            _browser.Add(GetViewListItem("movies-genres", MediaBrowserPlugin.UI.Resource.Genres));
            _browser.Add(GetViewListItem("movies-studios", MediaBrowserPlugin.UI.Resource.Studios));

            _browser.TotalCount = 5;
            _mre.Set();
        }

        protected ItemsByNameQuery GetItemsByNameQueryForMovie()
        {
            var query = new ItemsByNameQuery
                            {
                                SortBy = new string[] { ItemSortBy.SortName },
                                SortOrder=SortOrder.Ascending,
                                IncludeItemTypes = new string[] {"Movie"},
                                Recursive = true,
                                Fields = new ItemFields[] {ItemFields.ItemCounts, ItemFields.DateCreated},
                                UserId = GUIContext.Instance.Client.CurrentUserId
                            };

            return query;
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
            ShowItemsError(e);
            _mre.Set();
        }

        /// <summary>
        /// Load item details window
        /// </summary>
        /// <param name="item">The item.</param>
        protected void ShowDetails(GUIListItem item)
        {
            var details = item.TVTag as BaseItemDto;
            if (details != null) 
            {
                var parameters = new MediaBrowserItem{ Id = details.Id };
                GUICommon.Window(MediaBrowserWindow.Movie, parameters);
            }
        }

        /// <summary>
        /// Gets the unique identifier for the list item
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        protected string GetIdentifier(GUIListItem item)
        {
            if (item == null)
            {
                return string.Empty;
            }
            
            return item.Path;
        }

        /// <summary>
        /// Publishes the specified items.
        /// </summary>
        /// <param name="items">The items.</param>
        protected void Publish(List<GUIListItem> items)
        {
            _browser.Publish(SelectedId);
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
    }
}
