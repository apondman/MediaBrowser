using System.Linq;
using MediaBrowser.Model.Dto;
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using System;
using Pondman.MediaPortal.MediaBrowser.Models;
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
        public static readonly Action<MPGui.GUIListItem> ItemImageDownloadAndAssign =
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

        public static readonly Action<UserDto> UserPublishWorker = (user) =>
        {
            string prefix = MediaBrowserPlugin.DefaultProperty + ".User";
            string avatar = string.Empty;
            user.Publish(prefix);
            if (user.HasPrimaryImage)
            {
                // todo: let skin define dimensions
                avatar = GUIContext.Instance.Client.GetLocalUserImageUrl(user, new ImageOptions { Width = 60, Height = 60 });
            }
            GUIUtils.Publish(prefix + ".Avatar", avatar);
        };

        public static readonly Action<MPGui.GUIListItem> UserImageDownloadAndAssign =
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
            GUITask.MainThreadCallback(() => GUIWindowManager.ActivateWindow((int)window, Newtonsoft.Json.JsonConvert.SerializeObject(parameters)));
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
            if (!dto.Type.IsIn("Movie", "Episode", "Audio")) return;
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
            return dto.Type + "/" + dto.Id;
        }

    }
}
