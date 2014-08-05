using System.Collections.Generic;
using System.Windows.Media.Animation;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using MediaPortal.GUI.Library;
using Newtonsoft.Json;
using Pondman.MediaPortal.MediaBrowser.Models;
using System;
using System.Linq;
using System.Threading;
using MPGui = MediaPortal.GUI.Library;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq.Expressions;
using System.Net;

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
        private static readonly Dictionary<GUIControl, FacadeItemHandler> _itemHandlers;
        private static bool _notifyDisabledSmartControls = true;

        static GUICommon()
        {
            _itemHandlers = new Dictionary<GUIControl, FacadeItemHandler>();
        }

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
        public static async void RandomMovieCommand(GUIControl control, MPGui.Action.ActionType actionType)
        {
            if (GUIContext.Instance.HasActiveUser)
            {
                try
                {
                    var result = await GUIContext.Instance.Client.GetItemsAsync(MediaBrowserQueries.RandomMovie(GUIContext.Instance.ActiveUser.Id).Watched(false), CancellationToken.None);
                    Window(MediaBrowserWindow.Details, MediaBrowserMedia.Browse(result.Items.First().Id));
                }
                catch (Exception e)
                {
                    ShowRequestErrorDialog(e);
                }
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

        /// <summary>
        /// Scans for "smart" controls in the active window and initiates the handlers.
        /// </summary>
        /// <param name="window">The window.</param>
        internal static void SmartControlHandler(GUIWindow window)
        {
            if (!(MediaBrowserPlugin.Config.Settings.UseDefaultUser ?? false))
            {
                if (!_notifyDisabledSmartControls)
                {
                    MediaBrowserPlugin.Log.Warn("Smart controls are disabled because there is no default user active.");
                    _notifyDisabledSmartControls = false;
                }
                return;
            }
            
            // clearing old handlers
            _itemHandlers.Clear();

            if (RetryUntilSuccessOrTimeout(() => window.WindowLoaded, TimeSpan.FromSeconds(5)))
            {
                window.Children
                    .OfType<GUIFacadeControl>()
                    .Where(x => x.Description.StartsWith("MediaBrowser.Query."))
                    .AsParallel()
                    .ForAll(HandleFacade);

                MediaBrowserPlugin.Log.Debug("Handled smart facade controls.");
            }
            else
            {
                MediaBrowserPlugin.Log.Debug("Smart facade control detection timeout.");
            }
        }

        /// <summary>
        /// Reads  metadata, executes and populates a facade marked as smart control, 
        /// </summary>
        /// <param name="facade">The facade.</param>
        /// <returns></returns>
        internal async static void HandleFacade(GUIFacadeControl facade)
        {
            // break the facade description down into tokens
            var tokens = facade.Description.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);
            
            // if we have less than 4 segments there is not enough to be able to build a query
            if (tokens.Length < 4) return;

            // skin property
            var skinProperty = "#" + String.Join(".", tokens.Take(3).ToArray());

            // store custom identifier for skin properties
            var identifier = tokens[2];

            // create handler for this facade
            var handler = CreateFacadeHandler(facade);
            handler.Property = skinProperty;

            // publish loading message
            handler.SetLoading(true);

            // grab the default user id
            var userId = MediaBrowserPlugin.Config.Settings.DefaultUserId; //GUIContext.Instance.Client.CurrentUserId;

            // create default query
            var query = MediaBrowserQueries.Item.UserId(userId).Recursive();

            // build query, start with 4th token (token[3])
            var index = 3; var limit = 5;
            while (index < tokens.Length)
            {
                // MediaBrowser.Query.CustomIdentifier.Parameter1.Parameter2.Etc (.Limit as int)
                // Skin properties published are prefixed with: #MediaBrowser.Query.CustomIdentifier (.Loading, .Selected)
                //
                // examples: 
                // MediaBrowser.Query.MyDashboardQuery.RecentlyAdded.Movie.3  (shows 3 movies that were recently added)
                // MediaBrowser.Query.MyDashboardQuery.Random.Movie.5  (shows 5 random movies)
                // MediaBrowser.Query.MyDashboardQuery.RecentlyAdded.Episode.Unwatched.8  (shows 8 episodes that were recently added and unwatched)

                switch (tokens[index])
                {
                    // Item Types
                    case MediaBrowserType.MusicAlbum:
                        query = query.MusicAlbum();
                        break;
                    case MediaBrowserType.Audio:
                        query = query.Audio();
                        break;
                    case MediaBrowserType.Series:
                        query = query.Series();
                        break;
                    case MediaBrowserType.Season:
                        query = query.Season();
                        break;
                    case MediaBrowserType.Episode:
                        query = query.Episode();
                        break;
                    case MediaBrowserType.Movie:
                        query = query.Movies();
                        break;
                    // Item Filters
                    case "Dislikes":
                        query = query.Filters(ItemFilter.Dislikes);
                        break;
                    case "Likes":
                        query = query.Filters(ItemFilter.Likes);
                        break;
                    case "Favorite":
                        query = query.Filters(ItemFilter.IsFavorite);
                        break;
                    case "Resumable":
                        query = query.Filters(ItemFilter.IsResumable);
                        break;
                    case "Watched":
                        query = query.Filters(ItemFilter.IsPlayed);
                        break;
                    case "Unwatched":
                        query = query.Filters(ItemFilter.IsUnplayed);
                        break;
                    // Presets and sorting
                    case "RecentlyPlayed":
                        query = query.SortBy(ItemSortBy.DatePlayed, ItemSortBy.SortName).Descending();
                        break;
                    case "RecentlyAdded":
                        query = query.SortBy(ItemSortBy.DateCreated, ItemSortBy.SortName).Descending();
                        break;
                    case "Random":
                        query = query.SortBy(ItemSortBy.Random);
                        break;
                    case "SortName":
                        query = query.SortBy(ItemSortBy.SortName);
                        break;
                    case "DatePlayed":
                        query = query.SortBy(ItemSortBy.DatePlayed);
                        break;
                    case "Ascending":
                        query = query.Ascending();
                        break;
                    case "Descending":
                        query = query.Descending();
                        break;
                    default:
                        Int32.TryParse(tokens[index], out limit);
                        break;
                }

                index++;
            }

            // set limit
            query = query.Limit(limit);

            try
            {
                var result = await GUIContext.Instance.Client.GetItemsAsync(query, CancellationToken.None);
                facade.CycleLayout();   // pick first available layout
                facade.ClearAll();      // clear items;

                foreach (var item in result.Items.Select(dto => dto.ToListItem()))
                {
                    item.OnItemSelected += handler.DelayedItemHandler;
                    facade.Add(item);
                }

                facade.SelectIndex(0);
                MediaBrowserPlugin.Log.Debug("Loaded Smart Control: {0}, Items: {1}", identifier, result.Items.Length);
            }
            catch (Exception e)
            {
                MediaBrowserPlugin.Log.Error(e);
            }
            finally
            {
                handler.SetLoading(false);
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

        public static async void RetrieveImageData(GUIListItem item)
        {
            try
            {
                await Task.Factory.StartNew(() =>
                    {
                        var dto = item.TVTag as BaseItemDto;
                        if (dto != null && dto.HasPrimaryImage)
                        {

                            // todo: let skin define dimensions
                            string imageUrl = GUIContext.Instance.Client.GetLocalImageUrl(dto, new ImageOptions { Width = 200, Height = 300 }).Result;

                            if (!String.IsNullOrEmpty(imageUrl))
                            {
                                item.LoadImageFromMemory(imageUrl);
                            }
                        }
                    });
            }
            catch(Exception ex) 
            {
                MediaBrowserPlugin.Log.Error(ex);
            }
        }

        public static GUIListItem ToListItem(this BaseItemDto dto, BaseItemDto context = null)
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
            item.OnRetrieveArt += RetrieveImageData;

            item.Label = dto.ParseDtoProperties("Label", context);
            item.Label2 = dto.ParseDtoProperties("Label2", context);
            item.Label3 = dto.ParseDtoProperties("Label3", context);

            return item;

            //
            //switch (dto.Type)
            //{
            //    case MediaBrowserType.Audio:
            //        item.Label2 = dto.Artists != null ? String.Join(",", dto.Artists.ToArray()) : "Unknown";
            //        break;
            //}
            
        }

        internal static FacadeItemHandler CreateFacadeHandler(GUIFacadeControl facade)
        {
            var handler = new FacadeItemHandler(facade);
            _itemHandlers[facade] = handler;

            return handler;
        }

        public static string Variable<T>(Expression<Func<T>> reference)
        {
            var lambda = reference as LambdaExpression;
            var member = lambda.Body as MemberExpression;

            return "{" + member.Member.Name + "}";
        }

        public static string ParseDtoProperties(this BaseItemDto dto, string variable, BaseItemDto context = null)
        {
            string output = string.Empty;
            string define = "#MediaBrowser.List.";

            try 
            {
                List<string> variables = new List<string>();
                variables.Add(define + dto.Type + "." + variable);
                if (context != null) 
                {
                    variables.Insert(0, define + context.Type + "." + dto.Type + "." + variable);
                    variables.Insert(0, define + context.GetContext() + "." + dto.Type + "." + variable);
                    variables.Add(define + context.Type +  ".Default." + variable);
                }

                string property = define + "Default." + variable;
                foreach(string v in variables) 
                {
                    if (GUIPropertyManager.PropertyIsDefined(v)) 
                    {
                        property = v;
                        break;
                    }
                }               

                string pattern = GUIUtils.Read(property);
                output = pattern.Replace("_",":").FormatWith(GUIUtils.Culture, dto);
            }
            catch(Exception e) 
            {
                MediaBrowserPlugin.Log.Error(e);
                output = e.Message;
            }

            return output;
        }

    }
}
