using MediaBrowser.ApiInteraction.net35;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace Pondman.MediaPortal.MediaBrowser
{
    public class MediaBrowserClient : ApiClient
    {
        /// <summary>
        /// Occurs when the current user changes.
        /// </summary>
        public event Action<UserDto> CurrentUserChanged;
        
        public MediaBrowserClient(string serverHostName, int serverApiPort, string clientName, string deviceName, string deviceId, string version)
            : base(serverHostName, serverApiPort, clientName, deviceName, deviceId, version)
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
        /// Gets a value indicating whether a user is currently logged in.
        /// </summary>
        /// <value>
        /// <c>true</c> if a user logged in; otherwise, <c>false</c>.
        /// </value>
        public virtual bool IsUserLoggedIn
        {
            get
            {
                return (_currentUser != null);
            }
        }

        /// <summary>
        /// Gets the cached image URL for this item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        public virtual string GetLocalImageUrl(BaseItemDto item, ImageOptions options)
        {
            if (options.ImageType == ImageType.Backdrop) 
            {
                int index = options.ImageIndex != null ? (int)options.ImageIndex : 0;
                options.Tag = item.BackdropImageTags[index];
            } 
            else 
            {
                options.Tag = item.ImageTags[options.ImageType];
            }           

            return GetCachedImageUrl("items", options, () => GetImageUrl(item, options));
        }

        /// <summary>
        /// Gets the cached image URL for this user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        public virtual string GetLocalUserImageUrl(UserDto user, ImageOptions options) 
        {
            options.Tag = user.PrimaryImageTag;

            return GetCachedImageUrl("users", options, () => GetUserImageUrl(user, options));
        }

        /// <summary>
        /// Gets the cached image URL.
        /// </summary>
        /// <param name="folder">The folder.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="func">Delegate to retrieve the image.</param>
        /// <returns></returns>
        protected string GetCachedImageUrl(string subtype, ImageOptions options, Func<string> func)
        {
            string url = func();
            string filename = MD5(url);
            string folder = MediaBrowserPlugin.Settings.MediaCacheFolder + "\\" + subtype + "\\" + options.ImageType.ToString();
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
                            CopyStream(input, output);
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
        protected override void OnCurrentUserChanged()
        {
            base.OnCurrentUserChanged();

            if (CurrentUserChanged != null) 
            {
                CurrentUserChanged(CurrentUser);
            }
        }

        // todo: static methods below should be move to a more general location

        /// <summary>
        /// Creates an MD5 hash
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>hash</returns> 
        static string MD5(string input)
        {
            System.Security.Cryptography.MD5 _md5Hasher = System.Security.Cryptography.MD5.Create();
            byte[] data = _md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));
            return BitConverter.ToString(data).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Copies one stream to the other
        /// </summary>
        /// <param name="input">The input stream.</param>
        /// <param name="output">The output stream.</param>
        static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[8 * 1024];
            int len;
            while ( (len = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, len);
            }    
        }

    }
}
