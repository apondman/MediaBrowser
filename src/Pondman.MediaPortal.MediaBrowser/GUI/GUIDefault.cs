using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaPortal.GUI.Library;
using Pondman.MediaPortal.MediaBrowser.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using MPGUI = MediaPortal.GUI.Library;

namespace Pondman.MediaPortal.MediaBrowser.GUI
{
    public abstract class GUIDefault<TParameters> : GUIWindowX<TParameters>
        where TParameters : class, new()
    {
        static readonly Random _randomizer = new Random();

        protected AsyncImageResource _cover = null;
        protected ImageSwapper _backdrop = null;
        protected Dictionary<string, Action<GUIControl, MPGUI.Action.ActionType>> _commands = null;
        protected string _commandPrefix = MediaBrowserPlugin.DefaultName + ".Command.";

        [SkinControl(1)]
        protected GUIImage _backdropControl1 = null;

        [SkinControl(2)]
        protected GUIImage _backdropControl2 = null;
        
        public GUIDefault(MediaBrowserWindow window)
            : base(MediaBrowserPlugin.DefaultName + "." + window, (int)window)
        {
            MainTask = null;
            _logger = MediaBrowserPlugin.Log;
            _commands = new Dictionary<string, Action<GUIControl, MPGUI.Action.ActionType>>();

            // create backdrop image swapper
            _backdrop = new ImageSwapper
            {
                PropertyOne = MediaBrowserPlugin.DefaultProperty + ".Backdrop.1",
                PropertyTwo = MediaBrowserPlugin.DefaultProperty + ".Backdrop.2"
            };
            
            _backdrop.ImageResource.Delay = 0;

            // create cover image swapper
            _cover = new AsyncImageResource(MediaBrowserPlugin.Log)
            {
                Property = MediaBrowserPlugin.DefaultProperty + ".Coverart",
                Delay = 0
            };

            // auto register commands by convention
            //var commands = this.GetType().GetMethods().Where(m => m.Name.EndsWith("Command"));
            //foreach (var command in commands)
            //{
            //    string name = command.Name.Substring(0, command.Name.Length-7);
            //    RegisterCommand(name, ???);
            //}

            RegisterCommand("RandomMovie", GUICommon.RandomMovieCommand);
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            _backdrop.GUIImageOne = _backdropControl1;
            _backdrop.GUIImageTwo = _backdropControl2;

            Log.Debug("Attached backdrop controls.");

            // todo: move this somewhere more central? perhaps event handler on the service
            if (!GUIContext.Instance.IsServerReady)
            {
                GUIUtils.ShowOKDialog(MediaBrowserPlugin.UI.Resource.Error, MediaBrowserPlugin.UI.Resource.ServerNotFoundOnTheNetwork);
            }
            else {

                // Publish System Information
                GUIContext.Instance.Service.System.Publish(MediaBrowserPlugin.DefaultProperty + ".System");

                // Publish Default User
                GUIContext.Instance.PublishUser();
            }
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

            if (item.Type == "Movie")
            {
                PublishMovieDetails(item, prefix);
            }

            // Artwork
            PublishArtwork(item);
        }

        /// <summary>
        /// Publishes the movie details.
        /// </summary>
        /// <param name="movie">The movie.</param>
        /// <param name="prefix">The prefix.</param>
        protected virtual void PublishMovieDetails(BaseItemDto movie, string prefix = null)
        {
            // Check for prefix
            prefix = prefix ?? _publishPrefix;

            // Streams
            string streamPrefix = prefix + ".MediaStreams";
            GUIUtils.Unpublish(streamPrefix);
            if (movie.MediaStreams != null && movie.MediaStreams.Count > 0)
            {
                movie.MediaStreams.GroupBy(p => p.Type)
                    .ToDictionary(x => x.Key)
                    .Publish(streamPrefix);
            }

            // People
            string peoplePrefix = prefix + ".People";
            GUIUtils.Unpublish(peoplePrefix);
            if (movie.People != null && movie.People.Length > 0)
            {
                movie.People.GroupBy(p => p.Type)
                    .ToDictionary(x => x.Key)
                    .Publish(peoplePrefix);

                // People lists
                movie.People.GroupBy(p => p.Type)
                    .ToDictionary(x => x.Key + ".List", x => x.ToDelimited(s => s.Name))
                    .Publish(peoplePrefix);
            }

            // Lists
            (movie.Tags ?? Enumerable.Empty<string>()).ToDelimited().Publish(prefix + ".Tags.List");
            (movie.Genres ?? Enumerable.Empty<string>()).ToDelimited().Publish(prefix + ".Genres.List");
            (movie.Studios ?? Enumerable.Empty<StudioDto>()).ToDelimited(x => x.Name).Publish(prefix + ".Studios.List");

            // Runtime
            TimeSpan.FromTicks(movie.OriginalRunTimeTicks ?? 0).Publish(prefix + ".OriginalRuntime");
            TimeSpan.FromTicks(movie.RunTimeTicks ?? 0).Publish(prefix + ".Runtime");
        }

        /// <summary>
        /// Publishes the artwork.
        /// </summary>
        /// <param name="item">The item.</param>
        protected virtual void PublishArtwork(BaseItemDto item)
        {
            _cover.Filename = GetCoverUrl(item);
            _backdrop.Filename = GetBackdropUrl(item);
        }

        protected virtual string GetCoverUrl(BaseItemDto item)
        {
            return item.HasPrimaryImage ? GUIContext.Instance.Client.GetLocalImageUrl(item, new ImageOptions { Width = 277, Height = 400 }) : string.Empty;
        }

        protected virtual string GetBackdropUrl(BaseItemDto item)
        {
            return item.BackdropCount > 0 ? GUIContext.Instance.Client.GetLocalImageUrl(item, new ImageOptions { ImageType = ImageType.Backdrop, ImageIndex = _randomizer.Next(item.BackdropCount) }) : string.Empty;
        }

        protected void RegisterCommand(string name, Action<GUIControl, MPGUI.Action.ActionType> command) 
        {
            _commands[name] = command;
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
