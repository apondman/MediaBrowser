using System.Net.NetworkInformation;
using ConsoleApplication2.com.amazon.webservices;
using MediaBrowser.ApiInteraction.net35;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using System;
using System.IO;
using System.Net;
using System.Linq;
using MediaPortal.GUI.Library;

namespace Pondman.MediaPortal.MediaBrowser
{
    /// <summary>
    /// MediaBrowser API Client for MediaPortal
    /// </summary>
    public class MediaBrowserClient : ApiClient
    {
        const string CLIENT_NAME = "MediaPortal";
        
        /// <summary>
        /// Occurs when the current user changes.
        /// </summary>
        public event Action<UserDto> CurrentUserChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaBrowserClient"/> class.
        /// </summary>
        /// <param name="serverHostName">Name of the server host.</param>
        /// <param name="serverApiPort">The server API port.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <param name="deviceId">The device id.</param>
        /// <param name="version">The version.</param>
        public MediaBrowserClient(string serverHostName, int serverApiPort, string deviceName, string deviceId, string version)
            : base(serverHostName, serverApiPort, CLIENT_NAME, deviceName, deviceId, version)
        {
            // todo: logging
        }

        /// <summary>
        /// Gets or sets the current user.
        /// </summary>
        /// <value>
        /// The current user.
        /// </value>
        public virtual UserDto CurrentUser
        {
            get
            {
                return _currentUser;
            }
            set
            {
                _currentUser = value;
                CurrentUserId = _currentUser.Id;
            }
        } UserDto _currentUser;

        /// <summary>
        /// Gets a value indicating whether this instance is user logged in.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is user logged in; otherwise, <c>false</c>.
        /// </value>
        public virtual bool IsUserLoggedIn
        {
            get
            {
                return (_currentUser != null);
            }
        }

        /// <summary>
        /// Gets the local image URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        public virtual string GetLocalImageUrl(BaseItemDto item, ImageOptions options)
        {
            options.Tag = GetImageTag(item, options);

            return options.Tag != Guid.Empty ? GetCachedImageUrl("items", options, () => GetImageUrl(item, options)) : string.Empty;
        }

        /// <summary>
        /// Gets the local user image URL.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        public virtual string GetLocalUserImageUrl(UserDto user, ImageOptions options) 
        {
            options.Tag = user.PrimaryImageTag;

            return GetCachedImageUrl("users", options, () => GetUserImageUrl(user, options));
        }

        public virtual string GetLocalBackdropImageUrl(BaseItemDto item, ImageOptions options)
        {
            string[] urls = GetBackdropImageUrls(item, options);
            if (urls.Length == 0)
                return string.Empty;

            return GetCachedImageUrl("items", options, () => urls[options.ImageIndex ?? 0]);
        }


        /// <summary>
        /// Gets the cached image URL.
        /// </summary>
        /// <param name="subtype">The subtype.</param>
        /// <param name="options">The options.</param>
        /// <param name="func">The func.</param>
        /// <returns></returns>
        protected string GetCachedImageUrl(string subtype, ImageOptions options, Func<string> func)
        {
            string url = string.Empty;
            
            try
            {
                url = func();
            }
            catch (Exception e) 
            {
                Log.Error(e);
            }

            string filename = url.ToMd5Hash();
            string folder = MediaBrowserPlugin.Config.Settings.MediaCacheFolder + "\\" + subtype + "\\" + options.ImageType.ToString();
            string cachedPath = folder + "\\" + filename + ".jpg";
            
            try
            {
                if (!File.Exists(cachedPath))
                {
                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }

                    WebRequest webReq = WebRequest.Create(url);
                    WebResponse webResp = webReq.GetResponse();

                    using (FileStream output = File.OpenWrite(cachedPath))
                    {
                        using(Stream input = webResp.GetResponseStream()) 
                        {
                            input.CopyStream(output);
                        }
                    }
                }
            }
            catch(Exception) 
            {
                // todo: handle this better + logging
                try
                {
                    File.Delete(cachedPath);
                }
                catch (Exception)
                {

                }

                cachedPath = string.Empty;
            }

            return cachedPath;
        }

        /// <summary>
        /// Called when the current user changes.
        /// </summary>
        protected override void OnAuthorizationInfoChanged()
        {
            base.OnAuthorizationInfoChanged();

            if (CurrentUserChanged != null) 
            {
                CurrentUserChanged(CurrentUser);
            }
        }

        /// <summary>
        /// Gets the image tag.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        static Guid? GetImageTag(BaseItemDto item, ImageOptions options)
        {
            switch (options.ImageType)
            {
                case ImageType.Backdrop:
                    return item.BackdropCount > 0 ? item.BackdropImageTags[options.ImageIndex ?? 0] : null;
                case ImageType.Screenshot:
                    return item.ScreenshotCount > 0 ? item.ScreenshotImageTags[options.ImageIndex ?? 0] : null;
                case ImageType.Chapter:
                    return item.Chapters != null && item.Chapters.Count > 0
                        ? item.Chapters[options.ImageIndex ?? 0].ImageTag ?? null
                        : null;
                default:
                    Guid guid;
                    return item.ImageTags != null && item.ImageTags.TryGetValue(options.ImageType, out guid) ? guid : null;
            }
        }

    }
}
