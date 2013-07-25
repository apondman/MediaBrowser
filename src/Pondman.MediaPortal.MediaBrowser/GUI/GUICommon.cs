using MediaBrowser.Model.Dto;
using MediaPortal.GUI.Library;
using System;
using MPGui = MediaPortal.GUI.Library;

namespace Pondman.MediaPortal.MediaBrowser.GUI
{
    /// <summary>
    /// Enumeration of all the MediaBrowser related windows
    /// </summary>
    public enum MediaBrowserWindow
    {
        Main = 20130603,
        TVSeries = 201306031,
        Movie = 201306032,
        Episode = 201306033
    }

    public class MediaBrowserItem
    {
        public virtual string Type { get; set; }

        public virtual string Name { get; set; }

        public virtual string Id { get; set; }
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
                BaseItemDto dto = item.TVTag as BaseItemDto;
                if (dto.HasPrimaryImage)
                {
                    // todo: setup image options
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
                UserDto user = item.TVTag as UserDto;
                if (user.HasPrimaryImage)
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
            MPGui.GUIWindowManager.ActivateWindow((int)window, parameters);
        }

        /// <summary>
        ///  Wrapper for GUIWindowManager.ActivateWindow
        /// </summary>
        /// <typeparam name="TParameters">The type of the parameters.</typeparam>
        /// <param name="window">The window.</param>
        /// <param name="parameters">Parameter settings object</param>
        public static void Window<TParameters>(MediaBrowserWindow window, TParameters parameters)
        {
            MPGui.GUIWindowManager.ActivateWindow((int)window, Newtonsoft.Json.JsonConvert.SerializeObject(parameters));
        }

    }
}
