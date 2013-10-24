using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaPortal.GUI.Library;
using Pondman.MediaPortal.MediaBrowser.Models;
using Pondman.MediaPortal.MediaBrowser.Resources.Languages;
using Pondman.MediaPortal.MediaBrowser.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MPGUI = MediaPortal.GUI.Library;

namespace Pondman.MediaPortal.MediaBrowser.GUI
{
    public abstract class GUIDefault<TParameters> : GUIWindowX<TParameters>
        where TParameters : class, new()
    {
        static readonly Random _randomizer = new Random();

        protected readonly Dictionary<string, SmartImageControl> ImageResources;
        protected readonly ManualResetEvent _mre;
        protected ImageSwapper _backdrop = null;
        protected Dictionary<string, Action<GUIControl, MPGUI.Action.ActionType>> _commands = null;
        protected string _commandPrefix = MediaBrowserPlugin.DefaultName + ".Command.";

        [SkinControl(1)]
        protected GUIImage _backdropControl1 = null;

        [SkinControl(2)]
        protected GUIImage _backdropControl2 = null;
        
        protected GUIDefault(MediaBrowserWindow window)
            : base(MediaBrowserPlugin.DefaultName + "." + window, (int)window)
        {
            MainTask = null;
            _logger = MediaBrowserPlugin.Log;
            _commands = new Dictionary<string, Action<GUIControl, MPGUI.Action.ActionType>>();
            _mre = new ManualResetEvent(false);

            ImageResources = new Dictionary<string, SmartImageControl>();

            // create backdrop image swapper
            _backdrop = new ImageSwapper
            {
                PropertyOne = MediaBrowserPlugin.DefaultProperty + ".Backdrop.1",
                PropertyTwo = MediaBrowserPlugin.DefaultProperty + ".Backdrop.2"
            };
            
            _backdrop.ImageResource.Delay = 0;

            // auto register commands by convention
            //var commands = this.GetType().GetMethods().Where(m => m.Name.EndsWith("Command"));
            //foreach (var command in commands)
            //{
            //    string name = command.Name.Substring(0, command.Name.Length-7);
            //    RegisterCommand(name, ???);
            //}

            RegisterCommand("ChangeUser", ChangeUserCommand);
            RegisterCommand("RandomMovie", GUICommon.RandomMovieCommand);
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            // todo: move this somewhere more central? perhaps event handler on the service
            if (!GUIContext.Instance.IsServerReady)
            {
                GUIUtils.ShowOKDialog(MediaBrowserPlugin.UI.Resource.Error, MediaBrowserPlugin.UI.Resource.ServerNotFoundOnTheNetwork);
                GUIWindowManager.ShowPreviousWindow();
                return;
            }

            // if we are already logged in we are done
            if (GUIContext.Instance.Client.IsUserLoggedIn) return;

            // if not show user dialog
            ShowUserProfilesDialog();
        }

        protected override void OnWindowLoaded()
        {
            base.OnWindowLoaded();

            if (controlList != null)
            {
                var detected = controlList
                                .OfType<GUIImage>()
                                .Where(x => x.Description.StartsWith("MediaBrowser.Image."))
                                .Select(x =>
                                {
                                    var smart = (SmartImageControl) x;
                                    ImageResources[smart.Name] = smart;
                                    return false;
                                })
                                .Count();

                Log.Debug("Detected {0} smart image controls.", detected);
            }

            // Publish User Info
            GUIContext.Instance.PublishUser();

            _backdrop.GUIImageOne = _backdropControl1;
            _backdrop.GUIImageTwo = _backdropControl2;

            Log.Debug("Attached backdrop controls.");
        }

        protected override void OnClicked(int controlId, GUIControl control, MPGUI.Action.ActionType actionType)
        {
             // check whether the control contains a command
            if (control != null && (control.Description ?? string.Empty).StartsWith(_commandPrefix))
            {
                // parse the command name and check if it's registered.
                Action<GUIControl, MPGUI.Action.ActionType> action = null;
                var command = control.Description.Replace(_commandPrefix, string.Empty);
                if (!_commands.TryGetValue(command, out action)) return;

                Log.Debug("Command: {0}", command);

                // execute the command and return
                action(control, actionType);
                return;
            }

            // fallback to base implementation
            base.OnClicked(controlId, control, actionType);
        }

        /// <summary>
        /// Gets or sets the main task.
        /// </summary>
        /// <value>
        /// The main task.
        /// </value>
        public GUITask MainTask { get; set; }

        /// <summary>
        /// Gets a value indicating whether the main task is running.
        /// </summary>
        /// <value>
        /// <c>true</c> if the main task running; otherwise, <c>false</c>.
        /// </value>
        public bool IsMainTaskRunning
        {
            get
            {
                return (MainTask != null && !MainTask.IsCompleted);
            }
        }

        public virtual void ShowItemsError(Exception e)
        {
            GUIUtils.ShowOKDialog(MediaBrowserPlugin.UI.Resource.Error, MediaBrowserPlugin.UI.Resource.ErrorMakingRequest);
            Log.Error(e);
        }

        /// <summary>
        /// Publishes the item details.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="prefix">The prefix.</param>
        protected virtual void PublishItemDetails(BaseItemDto item, string prefix = null)
        {
            Log.Debug("Publishing {0}: {1}", item.Type, item.Name);

            // Check for prefix
            prefix = prefix ?? _publishPrefix;

            // Publish all properties
            item.Publish(prefix, "People", "MediaStreams");

            // Streams
            string streamPrefix = prefix + ".MediaStreams";
            if (item.MediaStreams != null && item.MediaStreams.Count > 0)
            {
                item.MediaStreams.GroupBy(p => p.Type)
                    .ToDictionary(x => x.Key)
                    .Publish(streamPrefix);
            }
            else
            {
                GUIUtils.Unpublish(streamPrefix);
            }

            // People
            string peoplePrefix = prefix + ".People";
            if (item.People != null && item.People.Length > 0)
            {
                //movie.People.GroupBy(p => p.Type)
                //    .ToDictionary(x => x.Key)
                //    .Publish(peoplePrefix);

                // People lists
                item.People.GroupBy(p => p.Type)
                    .ToDictionary(x => x.Key + ".List", x => x.ToDelimited(s => s.Name))
                    .Publish(peoplePrefix);
            }
            else
            {
                GUIUtils.Unpublish(peoplePrefix);
            }

            // Lists
            (item.Tags ?? Enumerable.Empty<string>()).ToDelimited().Publish(prefix + ".Tags.List");
            (item.Genres ?? Enumerable.Empty<string>()).ToDelimited().Publish(prefix + ".Genres.List");
            (item.Studios ?? Enumerable.Empty<StudioDto>()).ToDelimited(x => x.Name).Publish(prefix + ".Studios.List");

            // Runtime
            TimeSpan.FromTicks(item.OriginalRunTimeTicks ?? 0).Publish(prefix + ".OriginalRuntime");
            TimeSpan.FromTicks(item.RunTimeTicks ?? 0).Publish(prefix + ".Runtime");

            // Artwork
            PublishArtwork(item);
        }

        /// <summary>
        /// Publishes the artwork.
        /// </summary>
        /// <param name="item">The item.</param>
        protected virtual void PublishArtwork(BaseItemDto item)
        {
            _backdrop.Filename = GetBackdropUrl(item);

            if (ImageResources.Count == 0) return;

            SmartImageControl resource;
            if (ImageResources.TryGetValue(item.Type, out resource))
            {
                // load specific image
                resource.Resource.Filename = resource.GetImageUrl(item);
                return;
            }

            // load default image
            if (ImageResources.TryGetValue("Default", out resource))
            {
                resource.Resource.Filename = resource.GetImageUrl(item);
            }
        }

        protected virtual string GetBackdropUrl(BaseItemDto item)
        {
            var index = _randomizer.Next(item.BackdropCount);
            Log.Debug("Random Backdrop Index: {0} out of {1}", index, item.BackdropCount);
            return GUIContext.Instance.Client.GetLocalBackdropImageUrl(item, new ImageOptions { ImageType = ImageType.Backdrop, ImageIndex = index });
        }

        protected void RegisterCommand(string name, Action<GUIControl, MPGUI.Action.ActionType> command) 
        {
            _commands[name] = command;
        }

        /// <summary>
        ///     Switch User
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="actionType">Type of the action.</param>
        protected void ChangeUserCommand(GUIControl control, global::MediaPortal.GUI.Library.Action.ActionType actionType)
        {
            ShowUserProfilesDialog(forceSelect: true);
        }

        /// <summary>
        /// Shorthand for Translations
        /// </summary>
        /// <value>
        /// Translations
        /// </value>
        protected Translations T
        {
            get
            {
                return MediaBrowserPlugin.UI.Resource;
            }
        }

        /// <summary>
        /// Shows the user profiles dialog.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="forceSelect">if set to <c>true</c> [force select].</param>
        protected void ShowUserProfilesDialog(List<GUIListItem> items = null, bool forceSelect = false)
        {
            if (IsMainTaskRunning)
            {
                return;
            }

            if (items == null)
            {
                // Load user profiles from server
                // todo: improve loading
                MainTask = GUITask.Run(LoadUserProfiles, x => ShowUserProfilesDialog(x, forceSelect), MediaBrowserPlugin.Log.Error, true);
                return;
            }

            // get global settings shorthand
            var global = MediaBrowserPlugin.Config.Settings; 

            UserDto user = null;
            if (global.UseDefaultUser.HasValue && global.UseDefaultUser.Value)
            {
                user = items.Select(i => i.TVTag as UserDto).FirstOrDefault(u => u.Id == global.DefaultUserId);
            }

            // Decide whether we should show user select
            if (forceSelect || user == null)
            {
                // prompt user to make a selection
                var result = GUIUtils.ShowMenuDialog(MediaBrowserPlugin.UI.Resource.UserProfileLogin, items);
                
                // choice was cancelled, return
                if (result < 0) return;

                // choice was made, set user
                var item = items[result];
                user = item.TVTag as UserDto;
            }

            // if user is still null at this point return
            if (user == null) return;

            // load profile settings for user
            var profile = MediaBrowserPlugin.Config.Settings.ForUser(user.Id);
            var password = string.Empty;

            if (user.HasPassword)
            {
                // if user has a password check whether we already stored it, if not prompt the user for the password
                password = (profile.RememberMe ?? false) ? DataProtection.Decrypt(profile.Password) : GUIUtils.ShowKeyboard(string.Empty, true);
            }

            // authenticate user with the gathered data with the media browser server
            GUIContext.Instance.Client.AuthenticateUser(user.Id, password, success =>
            {
                if (success)
                {
                    
                    // if the user has a password but the remember me value has not been checked.
                    if (user.HasPassword && !profile.RememberMe.HasValue)
                    {
                        // ask user to remember the login
                        if (GUIUtils.ShowYesNoDialog(T.UserProfileRememberMe, T.UserProfileRememberMeText, true))
                        {
                            // store the encrypted password
                            var encrypted = DataProtection.Encrypt(password);
                            profile.RememberAuth(encrypted);
                        }
                    }

                    if (!global.UseDefaultUser.HasValue)
                    {
                        // if we don't have a default user set, ask the user if he wants to set this profile up as the default
                        if (GUIUtils.ShowYesNoDialog(T.UserProfileDefault, T.UserProfileDefaultText, true))
                        {
                            global.SetDefaultUser(user.Id);
                        }
                    }

                    // update the user context, reset views and continue
                    GUIContext.Instance.Client.CurrentUser = user;

                    if (user.HasPrimaryImage)
                    {
                        var avatar = ImageResources["User"];
                        avatar.Resource.Filename = GUIContext.Instance.Client.GetLocalUserImageUrl(user, new ImageOptions { Width = avatar.Width, Height = avatar.Height });
                    }

                    Reset();
                    return;
                }

                // show to the user that the login failed.
                GUIUtils.ShowOKDialog(MediaBrowserPlugin.UI.Resource.UserProfileLogin, MediaBrowserPlugin.UI.Resource.UserProfileLoginFailed);
                
                // forget auth because it has failed.
                profile.ForgetAuth();

                // reshow dialog
                ShowUserProfilesDialog(items);
            });
            
        }

        /// <summary>
        ///     Loads the user profiles from the server
        /// </summary>
        /// <param name="task">The task.</param>
        /// <returns></returns>
        protected virtual List<GUIListItem> LoadUserProfiles(GUITask task = null)
        {
            task = task ?? GUITask.None;
            var list = new List<GUIListItem>();

            WaitFor(x => GUIContext.Instance.Client.GetUsers(users =>
            {
                list.AddRange(users.TakeWhile(user => !task.IsCancelled).Select(GetUserListItem));
                x.Set();
            }, e =>
            {
                // todo: show error?
                Log.Error(e);
                x.Set();
            })
                ); // todo: timeout?

            return list;
        }

        /// <summary>
        ///     Gets the user list item.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns></returns>
        protected virtual GUIListItem GetUserListItem(UserDto user)
        {
            var item = new GUIListItem(user.Name)
            {
                Path = "User/" + user.Id,
                Label2 =
                    user.LastLoginDate.HasValue
                        ? String.Format("{0}: {1}", MediaBrowserPlugin.UI.Resource.LastSeen,
                            user.LastLoginDate.Value.ToShortDateString())
                        : string.Empty,
                TVTag = user,
                IconImage = "defaultPicture.png",
                IconImageBig = "defaultPictureBig.png",
                RetrieveArt = true
            };
            item.OnRetrieveArt += GetUserImage;

            return item;
        }

        /// <summary>
        ///     Gets an image for users
        /// </summary>
        /// <param name="item">The item.</param>
        public static void GetUserImage(GUIListItem item)
        {
            GUICommon.UserImageDownloadAndAssign.BeginInvoke(item, GUICommon.UserImageDownloadAndAssign.EndInvoke, null);
        }

        

        /// <summary>
        ///     Execute the given action and blocks using the ManualResetEvent.
        /// </summary>
        /// <param name="action">The action.</param>
        protected virtual void WaitFor(Action<ManualResetEvent> action)
        {
            _mre.Reset();
            action(_mre);
            _mre.WaitOne();
        }

        protected virtual void Reset()
        {
            GUIContext.Instance.PublishUser();
        }
    }

    public abstract class GUIDefault : GUIDefault<MediaBrowserItem>
    {
        protected GUIDefault(MediaBrowserWindow window)
            : base(window)
        {
            
        }
    } 
    
}
