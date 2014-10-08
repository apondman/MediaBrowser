using MediaBrowser.ApiInteraction;
using MediaBrowser.ApiInteraction.WebSocket;
using MediaBrowser.Model.ApiClient;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.Users;
using MediaPortal.GUI.Library;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pondman.MediaPortal.MediaBrowser
{
    public static class MediaBrowserClient
    {

        /// <summary>
        /// Gets the local image URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        public static async Task<string> GetLocalImageUrl(this IApiClient client, BaseItemDto item, ImageOptions options)
        {
            options.Tag = GetImageTag(item, options);

            return options.Tag != null ? await client.GetCachedImageUrl("items", options, () => client.GetImageUrl(item, options)) : string.Empty;
        }

        /// <summary>
        /// Gets the local user image URL.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        public static async Task<string> GetLocalUserImageUrl(this IApiClient client, UserDto user, ImageOptions options) 
        {
            options.Tag = user.PrimaryImageTag;

            return options.Tag != null ? await client.GetCachedImageUrl("users", options, () => client.GetUserImageUrl(user, options)) : string.Empty;
        }

        /// <summary>
        /// Gets the local backdrop image URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        public static async Task<string> GetLocalBackdropImageUrl(this IApiClient client, BaseItemDto item, ImageOptions options)
        {
            string[] urls = client.GetBackdropImageUrls(item, options);
            if (urls.Length == 0)
                return string.Empty;

            return await client.GetCachedImageUrl("items", options, () => urls[options.ImageIndex ?? 0]);
        }

        /// <summary>
        /// Gets the cached image URL.
        /// </summary>
        /// <param name="subtype">The subtype.</param>
        /// <param name="options">The options.</param>
        /// <param name="func">The func.</param>
        /// <returns></returns>
        public static async Task<string> GetCachedImageUrl(this IApiClient client, string subtype, ImageOptions options, Func<string> func)
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

                    using (var response = await client.GetImageStreamAsync(url, CancellationToken.None))
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

        public static async Task<AuthenticationResult> AuthenticateUserAsync(this IApiClient client, string username, string password)
        {
            using (SHA1 provider = SHA1.Create())
            {
                byte[] hash = provider.ComputeHash(Encoding.UTF8.GetBytes(password ?? string.Empty));

                return await client.AuthenticateUserAsync(username, hash);
            }
        } 
    }
}
