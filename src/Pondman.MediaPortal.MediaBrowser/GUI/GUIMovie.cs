using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using MediaBrowser.Model.Dto;
using MediaPortal.GUI.Library;
using MPGui = MediaPortal.GUI.Library;
using MediaBrowser.Model.Entities;

namespace Pondman.MediaPortal.MediaBrowser.GUI
{
    public class MediaBrowserMedia : MediaBrowserItem
    {
        public virtual bool Playback { get; set; }

        public static MediaBrowserMedia Browse(string id)
        {
            return new MediaBrowserMedia { Id = id };
        }

        public static MediaBrowserMedia Play(string id)
        {
            return new MediaBrowserMedia { Id = id, Playback = true };
        }
    }

    public class GUIMovie : GUIDefault<MediaBrowserMedia>
    {
        BaseItemDto _movie = null;
        MediaPlayer _player = null;
        
        #region Constructors

        public GUIMovie() : base(MediaBrowserWindow.Movie)
        {
            _player = new MediaPlayer(this, this._logger);
            _player.PlayerStarted += OnPlaybackStarted;

            RegisterCommand("Play", PlayCommand);
        }

        ~GUIMovie() 
        {
            _player.PlayerStarted -= OnPlaybackStarted;
        }

        #endregion

        protected override void OnPageLoad()
        {
            base.OnPageLoad();
            
            if (!GUIContext.Instance.IsServerReady) 
            {
                GUIWindowManager.ShowPreviousWindow();
                return;
            }            

            // Publish blank
            Unpublish();

            if (String.IsNullOrEmpty(Parameters.Id))
            {
                if (_movie != null)
                {
                    PublishMovieDetails(_movie);
                }
                else
                {
                    GUIWindowManager.ShowPreviousWindow();
                    return;
                }
            }
            else
            {
                // Load movie 
                GUITask.Run(LoadMovieDetails, PublishMovieDetailsTask, Log.Error);
            }
        }

        #region Commands

        protected void PlayCommand(GUIControl control, MPGui.Action.ActionType actionType)
        {
            Play();
        }

        #endregion       

        protected void Play() 
        {
            _player.Play(_movie.Path);
        }

        protected BaseItemDto LoadMovieDetails(GUITask task)
        {
            Log.Debug("Loading movie details for: {0}", Parameters.Id);
            
            ManualResetEvent mre = new ManualResetEvent(false);

            GUIContext.Instance.Client.GetItem(Parameters.Id, GUIContext.Instance.Client.CurrentUserId, (result) =>
            {
                _movie = result;
                mre.Set();
            }
            , (e) =>
            {
                Log.Error(e);
                mre.Set();
            });

            mre.WaitOne(); // todo: timeout?

            return _movie;
        }

        protected void PublishMovieDetailsTask(BaseItemDto movie)
        {
            PublishItemDetails(movie);
        }

        protected override void PublishMovieDetails(BaseItemDto movie, string prefix = null)
        {
            base.PublishMovieDetails(movie, prefix);

            if (Parameters.Playback) 
            {
                Parameters.Playback = false;
                GUITask.MainThreadCallback(Play);
            }
        }

        protected void OnPlaybackStarted(MediaPlayerInfo info)
        {
            info.Title = _movie.Name;
            info.Year = _movie.ProductionYear.ToString();
            info.Plot = _movie.Overview;
            info.Genre = _movie.Genres.FirstOrDefault();
            info.Thumb = _cover.Filename;
        }
    }
}
