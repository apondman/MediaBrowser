using MediaBrowser.ApiInteraction;
using MediaBrowser.ApiInteraction.WebSocket;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Users;
using MediaPortal.GUI.Library;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        public MediaBrowserClient(string serverAddress, string deviceName, string deviceId, string version)
            : base(new MediaBrowserLogger(MediaBrowserPlugin.Log), serverAddress, CLIENT_NAME, deviceName, deviceId, version)
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
        public async Task<string> GetLocalImageUrl(BaseItemDto item, ImageOptions options)
        {
            options.Tag = GetImageTag(item, options);

            return options.Tag != null ? await GetCachedImageUrl("items", options, () => GetImageUrl(item, options)) : string.Empty;
        }

        /// <summary>
        /// Gets the local user image URL.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        public async Task<string> GetLocalUserImageUrl(UserDto user, ImageOptions options) 
        {
            options.Tag = user.PrimaryImageTag;

            return options.Tag != null ? await GetCachedImageUrl("users", options, () => GetUserImageUrl(user, options)) : string.Empty;
        }

        /// <summary>
        /// Gets the local backdrop image URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        public async Task<string> GetLocalBackdropImageUrl(BaseItemDto item, ImageOptions options)
        {
            string[] urls = GetBackdropImageUrls(item, options);
            if (urls.Length == 0)
                return string.Empty;

            return await GetCachedImageUrl("items", options, () => urls[options.ImageIndex ?? 0]);
        }

        /// <summary>
        /// Gets the cached image URL.
        /// </summary>
        /// <param name="subtype">The subtype.</param>
        /// <param name="options">The options.</param>
        /// <param name="func">The func.</param>
        /// <returns></returns>
        protected async Task<string> GetCachedImageUrl(string subtype, ImageOptions options, Func<string> func)
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

                    using (var response = await HttpClient.GetAsync(url, CancellationToken.None))
                    {
                        
                        using (FileStream output = File.OpenWrite(cachedPath))
                        {
                            response.CopyStream(output);
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
        protected void OnAuthorizationInfoChanged()
        {
           //base.OnAuthorizationInfoChanged();
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
        static string GetImageTag(BaseItemDto item, ImageOptions options)
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
                    string guid;
                    return item.ImageTags != null && item.ImageTags.TryGetValue(options.ImageType, out guid) ? guid : null;
            }
        }

        public async Task<AuthenticationResult> AuthenticateUserAsync(string username, string password)
        {
            using (SHA1 provider = SHA1.Create())
            {
                byte[] hash = provider.ComputeHash(Encoding.UTF8.GetBytes(password ?? string.Empty));
                return await AuthenticateUserAsync(username, hash);
            }
        }

        public async Task<ItemsResult> GetItemsByNameAsync(string name, ItemsByNameQuery query)
        {
            var url = GetItemByNameListUrl(name, query);

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<ItemsResult>(stream);
            }
        }

    }
}
