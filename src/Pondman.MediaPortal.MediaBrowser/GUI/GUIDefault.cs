using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MediaPortal.GUI.Library;
using Pondman.MediaPortal.MediaBrowser.Models;
using Pondman.MediaPortal.MediaBrowser.Resources.Languages;
using Pondman.MediaPortal.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MPGUI = MediaPortal.GUI.Library;

namespace Pondman.MediaPortal.MediaBrowser.GUI
{
    public abstract class GUIDefault<TParameters> : GUIWindowX<TParameters>
        where TParameters : class, new()
    {
        static readonly Random _randomizer = new Random();

        protected readonly ConcurrentDictionary<string, List<SmartImageControl>> _smartImageControls;
        protected Dictionary<string, Action<GUIControl, MPGUI.Action.ActionType>> _commands = null;
        protected string _commandPrefix = MediaBrowserPlugin.DefaultName + ".Command.";

        [SkinControl(1)]
        protected GUIImage _backdropControl1 = null;

        [SkinControl(2)]
        protected GUIImage _backdropControl2 = null;

        protected ImageSwapper backdropHandler;

        protected GUIDefault(MediaBrowserWindow window)
            : base(MediaBrowserPlugin.DefaultName + "." + window, (int)window)
        {
            MainTask = null;
            _logger = MediaBrowserPlugin.Log;
            _commands = new Dictionary<string, Action<GUIControl, MPGUI.Action.ActionType>>();

            _smartImageControls = new ConcurrentDictionary<string, List<SmartImageControl>>();

            // auto register commands by convention
            //var commands = this.GetType().GetMethods().Where(m => m.Name.EndsWith("Command"));
            //foreach (var command in commands)
            //{
            //    string name = command.Name.Substring(0, command.Name.Length-7);
            //    RegisterCommand(name, ???);
            //}

            RegisterCommand("ChangeUser", ChangeUserCommand);
            RegisterCommand("RandomMovie", GUICommon.RandomMovieCommand);

            backdropHandler = new ImageSwapper
                            {
                                PropertyOne = MediaBrowserPlugin.DefaultProperty + ".Backdrop.1",
                                PropertyTwo = MediaBrowserPlugin.DefaultProperty + ".Backdrop.2"
                            };

            backdropHandler.ImageResource.Delay = 0;
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

        protected void RegisterSmartImageControl(SmartImageControl control) 
        {
            List<SmartImageControl> list;
            if (!_smartImageControls.TryGetValue(control.Name, out list))
            {
                list = new List<SmartImageControl>();
                _smartImageControls[control.Name] = list;
            }
            list.Add(control);
        }

        protected List<SmartImageControl> GetSmartImageControls(BaseItemDto item)
        {
            return GetSmartImageControlsByType(item.Type);
        }

        protected List<SmartImageControl> GetSmartImageControlsByType(string type)
        {
            List<SmartImageControl> list;
            if (!_smartImageControls.TryGetValue(type, out list))
            {
                if (!_smartImageControls.TryGetValue("Default", out list))
                {
                    list = new List<SmartImageControl>();
                }
            }

            return list;
        }

        protected void UpdateSmartImageControls(BaseItemDto item) 
        {
            if (_smartImageControls.Count == 0) return;

            var controls = GetSmartImageControls(item);
            foreach (var control in controls)
            {
                control.LoadAsync(item);
            }
        }        

        protected override void OnWindowLoaded()
        {
            base.OnWindowLoaded();

            if (controlList != null)
            {
                var detected = controlList
                                .OfType<GUIImage>()
                                .Where(x => x.Description.StartsWith("MediaBrowser.Image."))
                                .Select(x => (SmartImageControl)x)
                                .ToList();

                detected.ForEach(RegisterSmartImageControl);

                Log.Debug("Detected {0} smart image controls.", detected.Count);
            }

            // Publish User Info
            GUIContext.Instance.PublishUser();

            backdropHandler.GUIImageOne = _backdropControl1;
            backdropHandler.GUIImageTwo = _backdropControl2;
            backdropHandler.Filename = "";

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
        protected virtual async Task PublishItemDetails(BaseItemDto item, string prefix = null)
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
            //TimeSpan.FromTicks(item.OriginalRunTimeTicks ?? 0).Publish(prefix + ".OriginalRuntime");
            TimeSpan.FromTicks(item.RunTimeTicks ?? 0).Publish(prefix + ".Runtime");

            // Artwork
            await PublishArtwork(item);
        }

        /// <summary>
        /// Publishes the artwork.
        /// </summary>
        /// <param name="item">The item.</param>
        protected virtual async Task PublishArtwork(BaseItemDto item)
        {
            var backdrop = await GetBackdropUrl(item);
            backdropHandler.Filename = backdrop;

            UpdateSmartImageControls(item);
        }

        protected virtual async Task<string> GetBackdropUrl(BaseItemDto item)
        {
            var index = _randomizer.Next(item.BackdropCount); // todo: use random setting
            Log.Debug("Random Backdrop Index: {0} out of {1}", index, item.BackdropCount);
            return await GUIContext.Instance.Client.GetLocalBackdropImageUrl(item, new ImageOptions { ImageType = ImageType.Backdrop, ImageIndex = index });
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
        protected async void ShowUserProfilesDialog(List<GUIListItem> items = null, bool forceSelect = false)
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
            var auth = await GUIContext.Instance.Client.AuthenticateUserAsync(user.Name, password);
            if (auth.User != null) 
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
                    var avatars = GetSmartImageControlsByType("User");
                    foreach(var avatar in avatars) 
                    {
                        avatar.Resource.Filename = await GUIContext.Instance.Client.GetLocalUserImageUrl(user, new ImageOptions { Width = avatar.Width, Height = avatar.Height, ImageType = avatar.ImageType });
                    }
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

            try
            {
                var subtask = GUIContext.Instance.Client.GetUsersAsync(new UserQuery());
                var users = subtask.Result;
                list.AddRange(users.TakeWhile(user => !task.IsCancelled).Select(GetUserListItem));
            }
            catch (Exception e)
            {
                Log.Error(e);
            }          

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
        public static async void GetUserImage(GUIListItem item)
        {
            var user = item.TVTag as UserDto;
            if (user != null && user.HasPrimaryImage)
            {
                // todo: setup image options
                string imageUrl = await GUIContext.Instance.Client.GetLocalUserImageUrl(user, new ImageOptions());
                if (!String.IsNullOrEmpty(imageUrl))
                {
                    item.IconImage = imageUrl;
                    item.IconImageBig = imageUrl;
                }
            }
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
