using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Services;
using MediaPortal.GUI.Library;
using MPGui = MediaPortal.GUI.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using System.Threading;
using System.Net;
using System.Drawing.Imaging;
using System.Drawing;
using Pondman.MediaPortal.GUI;
using System.Windows.Media.Animation;
using MediaBrowser.Model.Entities;

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

        ~GUIMain() { }

        #region Controls
        
        [SkinControl(50)]
        protected GUIFacadeControl facade = null;

        #endregion

        #region Commands

        protected void CycleLayoutCommand(GUIControl control, MPGui.Action.ActionType actionType)
        {
            facade.CycleLayout();
            facade.Focus();
            Log.Debug("Layout: {0}", facade.CurrentLayout);
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

            _browser.Attach(facade);

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
                            Navigate(facade.SelectedListItem);
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

        protected void OnBaseItemSelected(GUIListItem item)
        {
            if (item != null)
            {
                // todo: check for specific dto
                BaseItemDto dto = item.TVTag as BaseItemDto;
                PublishItemDetails(dto, MediaBrowserPlugin.DefaultProperty + ".Selected");
            }
        }

        protected void OnPublishCurrent(GUIListItem item)
        {
            if (item != null)
            {
                BaseItemDto dto = item.TVTag as BaseItemDto;
                if (dto != null)
                {
                    PublishItemDetails(dto, MediaBrowserPlugin.DefaultProperty + ".Current");
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
            BaseItemDto view = new BaseItemDto();
            view.Name = label; 
            view.Id = id; 
            view.Type = "View";
            view.IsFolder = true;

            return GetBaseListItem(view);
        }

        /// <summary>
        /// Gets the base list item.
        /// </summary>
        /// <param name="dto">The dto.</param>
        /// <returns></returns>
        public GUIListItem GetBaseListItem(BaseItemDto dto)
        {
            GUIListItem item = new GUIListItem(dto.Name);
            item.Path = dto.Type + "/" + dto.Id;
            item.Year = dto.ProductionYear ?? 0;
            item.TVTag = dto;
            item.IsFolder = dto.IsFolder;
            item.IconImage = "defaultVideo.png";
            item.IconImageBig = "defaultVideoBig.png";
            item.RetrieveArt = true;
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
            // todo: cache list item
            var item = new GUIListItem(user.Name);
            item.Path = "User/" + user.Id;
            item.Label2 = user.LastLoginDate.HasValue ? "Last seen: " + user.LastLoginDate.Value.ToShortDateString() : string.Empty; // todo: translate
            item.TVTag = user;
            item.IconImage = "defaultPicture.png";
            item.IconImageBig = "defaultPictureBig.png";
            item.RetrieveArt = true;
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
        /// Resets the  navigation to the starting position
        /// </summary>
        public void Reset()
        {
            GUIContext.Instance.PublishUser();

            _history.Clear();
            SelectedId = null;

            // get root folder
            GUIContext.Instance.Client.GetRootFolder(GUIContext.Instance.Client.CurrentUserId, LoadRoot, ShowItemsError);
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
                else
                {
                    current = _history.Pop();
                }
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
            if (item == null || item.IsFolder)
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
        protected void LoadRoot(BaseItemDto dto)
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
            GUIListItem currentItem = _history.Peek();
            
            // clear browser and set current
            _browser.Current(currentItem);

            var itemInfo = GetMediaBrowserItemFromPath(currentItem);
            if (itemInfo.Type == "UserRootFolder")
            {
                // Default views and queries
                //_browser.Add(GetViewListItem("movies", MediaBrowserPlugin.UI.Resource.Movies));
                //_browser.Add(GetViewListItem("tvshows", MediaBrowserPlugin.UI.Resource.TVShows));
                //_browser.TotalCount = 2;
                //return null;
            //}
            //else if (itemInfo.Type == "View" && itemInfo.Id == "movies")
            //{
                _browser.Add(GetViewListItem("movies-latest", MediaBrowserPlugin.UI.Resource.LatestUnwatchedMovies));
                _browser.Add(GetViewListItem("movies-all", MediaBrowserPlugin.UI.Resource.AllMovies));
                _browser.Add(GetViewListItem("movies-genres", MediaBrowserPlugin.UI.Resource.Genres));
                _browser.Add(GetViewListItem("movies-studios", MediaBrowserPlugin.UI.Resource.Studios));
                _browser.TotalCount = 4;
                return null;
            }

            // we are using the manual reset event because the call on the client is async and we want this method to only complete when it's done.
            _mre.Reset();

            // get the query
            var query = GetItemQuery(itemInfo);
            GUIContext.Instance.Client.GetItems(query, PopulateBrowser, ShowItemsErrorAndRelease);

            _mre.WaitOne(); // todo: timeout?

            return null;
        }

        protected void PopulateBrowser(ItemsResult result)
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

        protected void ShowItemsErrorAndRelease(Exception e)
        {
            ShowItemsError(e);
            _mre.Set();
        }

        protected MediaBrowserItem GetMediaBrowserItemFromPath(GUIListItem item)
        {
            return GetMediaBrowserItemFromPath(item.Path);
        }

        protected MediaBrowserItem GetMediaBrowserItemFromPath(string path)
        {
            string[] tokens = path.Split('/');
            return new MediaBrowserItem { Type = tokens[0], Id = tokens[1] };
        }

        protected ItemQuery GetItemQuery(GUIListItem item)
        {
            return GetItemQuery(item.Path);
        }

        protected ItemQuery GetItemQuery(string path) {
            return GetItemQuery(GetMediaBrowserItemFromPath(path));
        }

        protected ItemQuery GetItemQuery(MediaBrowserItem item)
        {
            string userId = GUIContext.Instance.Client.CurrentUserId;

            if (item.Type == "View")
            {
                var query = MediaBrowserQueries.New
                            .UserId(userId)
                            .Recursive()
                            .Fields(ItemFields.Overview, ItemFields.People, ItemFields.Genres, ItemFields.MediaStreams);

                switch (item.Id)
                {
                    case "movies-latest":
                        return query
                                .Movies()
                                .SortBy(ItemSortBy.DateCreated)
                                .Filters(ItemFilter.IsUnplayed)
                                .Descending()
                                .Limit(100);
                        //Genres?SortBy=SortName&SortOrder=Ascending&IncludeItemTypes=Movie%2CTrailer&Recursive=true&Fields=ItemCounts%2CDateCreated%2CUserData&StartIndex=0&Limit=300&userId=78ee3c8192698ab53354b66650dc8201
                    case "movies-all":
                        return query
                                .Movies()
                                .SortBy(ItemSortBy.SortName)
                                .Ascending();
                                
                    case "tvshows":
                        return query
                                .TVShows()
                                .SortBy(ItemSortBy.SortName)
                                .Ascending();
                }
            }

            return null;
        }

        /// <summary>
        /// Load item details window
        /// </summary>
        /// <param name="item">The item.</param>
        protected void ShowDetails(GUIListItem item)
        {
            BaseItemDto details = item.TVTag as BaseItemDto;
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

            int result = GUIUtils.ShowMenuDialog(MediaBrowserPlugin.UI.Resource.UserProfileLogin, items);
            if (result > -1)
            {
                var item = items[result];
                UserDto user = item.TVTag as UserDto;

                string password = user.HasPassword ? GUIUtils.ShowKeyboard(string.Empty, true) : string.Empty;

                GUIContext.Instance.Client.AuthenticateUser(user.Id, password, (success) =>
                {
                    if (success)
                    {
                        GUIContext.Instance.Client.CurrentUser = user;
                        Reset();
                        return;
                    }
                    else
                    {
                        GUIUtils.ShowOKDialog(MediaBrowserPlugin.UI.Resource.UserProfileLogin, MediaBrowserPlugin.UI.Resource.UserProfileLoginFailed);
                        ShowUserProfilesDialog(items);
                    }
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
            ManualResetEvent mre = new ManualResetEvent(false);
            List<GUIListItem> list = new List<GUIListItem>();

            GUIContext.Instance.Client.GetUsers((users) =>
            {
                foreach (var user in users)
                {
                    if (task.IsCancelled)
                    {
                        break;
                    }

                    // get user list item and add it to the facade
                    var item = GetUserListItem(user);
                    list.Add(item);
                }

                mre.Set();
            }
            , (e) =>
            {
                // todo: show error?
                Log.Error(e);
                mre.Set();
            });

            mre.WaitOne(); // todo: timeout?

            return list;
        }

        protected override void PublishArtwork(BaseItemDto item)
        {
            string cover = GetCoverUrl(item);
            string backdrop = GetBackdropUrl(item);

            // todo: need a better way to do this 
            // the methods above are blocking (downloading and creating cache worst case and once they return 
            // the selection could be changed so we quickly check whether the image is still relevant
            if (facade != null && facade.SelectedListItem != null && facade.SelectedListItem.TVTag == item)
            {
               _cover.Filename = cover;
               _backdrop.Filename = backdrop;
            }            
        }
    }
}
