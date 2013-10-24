using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using MediaPortal.GUI.Library;
using Newtonsoft.Json;
using Pondman.MediaPortal.MediaBrowser.Models;
using System;
using System.Linq;
using System.Threading;
using MPGui = MediaPortal.GUI.Library;

namespace Pondman.MediaPortal.MediaBrowser.GUI
{
    /// <summary>
    /// Enumeration of all the MediaBrowser related windows
    /// </summary>
    public enum MediaBrowserWindow
    {
        Main = 20130603,
        Details = 201306032,
    }

    /// <summary>
    /// Collection of common methods used by all the GUIWindows
    /// </summary>
    public static class GUICommon 
    {
        /// <summary>
        /// Delegate to perform an image download  and assign it to a gui listitem
        /// </summary>
        public static readonly Action<GUIListItem> ItemImageDownloadAndAssign =
            (item) =>
            {
                var dto = item.TVTag as BaseItemDto;
                if (dto != null && dto.HasPrimaryImage)
                {
                    // todo: let skin define dimensions
                    string imageUrl = GUIContext.Instance.Client.GetLocalImageUrl(dto, new ImageOptions { Width = 200, Height = 300 });

                    if (!String.IsNullOrEmpty(imageUrl))
                    {
                        item.IconImage = imageUrl;
                        item.IconImageBig = imageUrl;
                    }
                }
            };

        public static readonly Action<UserDto> UserPublishWorker = (user) => user.Publish(MediaBrowserPlugin.DefaultProperty + ".User");

        public static readonly Action<GUIListItem> UserImageDownloadAndAssign =
            (item) =>
            {
                var user = item.TVTag as UserDto;
                if (user != null && user.HasPrimaryImage)
                {
                    // todo: setup image options
                    string imageUrl = GUIContext.Instance.Client.GetLocalUserImageUrl(user, new ImageOptions());
                    if (!String.IsNullOrEmpty(imageUrl))
                    {
                        item.IconImage = imageUrl;
                        item.IconImageBig = imageUrl;
                    }
                }
            };

        /// <summary>
        /// Wrapper for GUIWindowManager.ActivateWindow
        /// </summary>
        /// <param name="window">The window.</param>
        /// <param name="parameters">The parameters.</param>
        public static void Window(MediaBrowserWindow window, string parameters = null)
        {
            GUITask.MainThreadCallback(() => GUIWindowManager.ActivateWindow((int)window, parameters));  
        }

        /// <summary>
        ///  Wrapper for GUIWindowManager.ActivateWindow
        /// </summary>
        /// <typeparam name="TParameters">The type of the parameters.</typeparam>
        /// <param name="window">The window.</param>
        /// <param name="parameters">Parameter settings object</param>
        public static void Window<TParameters>(MediaBrowserWindow window, TParameters parameters)
        {
            GUITask.MainThreadCallback(() => GUIWindowManager.ActivateWindow((int)window, JsonConvert.SerializeObject(parameters)));
        }

        /// <summary>
        /// Go to a random movie for the active user.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="actionType">Type of the action.</param>
        public static void RandomMovieCommand(GUIControl control, MPGui.Action.ActionType actionType)
        {
            if (GUIContext.Instance.HasActiveUser)
            {
                GUIContext.Instance.Client.GetItems(MediaBrowserQueries.RandomMovie(GUIContext.Instance.ActiveUser.Id).Watched(false),
                    result => Window(MediaBrowserWindow.Details, MediaBrowserMedia.Browse(result.Items.First().Id))
                    , ShowRequestErrorDialog);
            }
        }

        /// <summary>
        /// Shows an error dialog and logs and error.
        /// </summary>
        /// <param name="e">The exception</param>
        public static void ShowRequestErrorDialog(Exception e)
        {
            GUIUtils.ShowOKDialog(MediaBrowserPlugin.UI.Resource.Error, MediaBrowserPlugin.UI.Resource.ErrorMakingRequest);
            MediaBrowserPlugin.Log.Error(e);
        }

        /// <summary>
        /// Jumps to the detail window for the specific base item
        /// </summary>
        /// <param name="dto">The dto.</param>
        public static void ViewDetails(BaseItemDto dto)
        {
            if (!dto.Type.IsIn(MediaBrowserType.Movie, MediaBrowserType.Episode,MediaBrowserType.Audio)) return;
            var parameters = new MediaBrowserItem {Id = dto.Id};
            Window(MediaBrowserWindow.Details, parameters);
        }

        /// <summary>
        /// Gets an identifier for the context
        /// </summary>
        /// <param name="dto">The dto.</param>
        /// <returns></returns>
        public static string GetContext(this BaseItemDto dto)
        {
            // todo: this will have to change
            return dto.Type == "View" ? dto.Type + "/" + dto.Id : dto.Type;
        }

        internal static readonly Action<GUIWindow> HandleSmartControls = SmartControlHandler;

        internal static void SmartControlHandler(GUIWindow window)
        {
            if (RetryUntilSuccessOrTimeout(() => window.WindowLoaded, TimeSpan.FromSeconds(5)))
            {
                var facades = window.Children
                    .OfType<GUIFacadeControl>()
                    .Where(x => x.Description.StartsWith("MediaBrowser.Query."))
                    .Select(x =>
                    {
                        var tokens = x.Description.Split(new string[] {"."}, StringSplitOptions.RemoveEmptyEntries);
                        return ProcessQuery(x, tokens);
                    }).Count();

                MediaBrowserPlugin.Log.Debug("Detected {0} smart facade controls.", facades);
            }
            else
            {
                MediaBrowserPlugin.Log.Debug("Smart facade control detection timeout.");
            }
        }

        internal static bool ProcessQuery(GUIFacadeControl facade, string[] tokens)
        {
            var userId = MediaBrowserPlugin.Config.Settings.DefaultUserId; //GUIContext.Instance.Client.CurrentUserId;
            var index = 2;
            ItemQuery query = null;

            while (true)
            {
                // MediaBrowser.Query.Type.Query.Limit // SortBy.Direction.Limit
                // MediaBrowser.Query.Movie.RecentlyAdded.5 // DateAdded.Descending

                switch (tokens[index])
                {
                    case "Movie":
                        query = MediaBrowserQueries.Item.UserId(userId).Movies().Recursive();
                        break;
                    case "RecentlyAdded":
                        query = query.SortBy(ItemSortBy.DateCreated, ItemSortBy.SortName).Descending();
                        break;
                }

                if (index < 3)
                {
                    index++;
                    continue;
                }
               
                int limit = 10;
                if (tokens.Length == 5) Int32.TryParse(tokens[4], out limit);
                query = query.Limit(limit);

                GUIContext.Instance.Client.GetItems(query, result => 
                {
                    facade.CycleLayout();

                    MediaBrowserPlugin.Log.Debug("DASHBOARD LAYOUT: {0}", facade.CurrentLayout);

                    facade.ClearAll();

                    foreach (var dto in result.Items)
                    {
                        var item = dto.ToListItem();
                        facade.Add(item);
                    }

                    facade.SelectIndex(0);
                    //facade.Visible(true);
                    //facade.Focus();

                    MediaBrowserPlugin.Log.Debug("DASHBOARD LOADED: {0}", facade.Visible);
                }, MediaBrowserPlugin.Log.Error);

                return true;
            }
        }

        internal static bool RetryUntilSuccessOrTimeout(Func<bool> task, TimeSpan timeSpan)
        {
            bool success = false;
            int elapsed = 0;
            while ((!success) && (elapsed < timeSpan.TotalMilliseconds))
            {
                Thread.Sleep(1000);
                elapsed += 1000;
                success = task();
            }

            return success;
        }

        public static void GetItemImage(GUIListItem item)
        {
            ItemImageDownloadAndAssign.BeginInvoke(item, ItemImageDownloadAndAssign.EndInvoke, null);
        }

        public static GUIListItem ToListItem(this BaseItemDto dto, BaseItemDto context = null)
        {
            return GetBaseListItem(dto, context);
        }

        public static GUIListItem GetBaseListItem(BaseItemDto dto, BaseItemDto context = null)
        {
            var item = new GUIListItem(dto.Name)
            {
                ItemId = (dto.Type + "/" + dto.Id).GetHashCode(),
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
                    if (context != null && context.Type.IsIn(MediaBrowserType.View))
                    {
                        item.Label = dto.SeriesName + String.Format(" - {0}x{1} - {2}", dto.ParentIndexNumber ?? 0, dto.IndexNumber ?? 0, item.Label);
                    }
                    else
                    {
                        item.Label = String.Format("{0}: {1}", dto.IndexNumber ?? 0, item.Label);
                    }
                    item.Label2 = dto.PremiereDate.HasValue
                        ? dto.PremiereDate.Value.ToString(GUIUtils.Culture.DateTimeFormat.ShortDatePattern)
                        : String.Empty;
                    break;
                case "Season":
                case "BoxSet":
                    item.Label2 = dto.ChildCount.HasValue ? dto.ChildCount.ToString() : String.Empty;
                    break;
                case "Artist":
                    item.Label2 = String.Format("{0}/{1}", (dto.AlbumCount ?? 0), (dto.SongCount ?? 0));
                    break;
                case "Person":
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
                default:
                    item.Label2 = dto.ProductionYear.HasValue ? dto.ProductionYear.ToString() : String.Empty;
                    break;
            }
            return item;
        }
    }
}
