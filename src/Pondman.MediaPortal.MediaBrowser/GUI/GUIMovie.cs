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
using Pondman.MediaPortal.MediaBrowser.Models;

namespace Pondman.MediaPortal.MediaBrowser.GUI
{
    public class GUIMovie : GUIDefault<MediaBrowserMedia>
    {
        BaseItemDto _movie = null;
        MediaPlayer _player = null;
        
        #region Constructors

        public GUIMovie() : base(MediaBrowserWindow.Movie)
        {
            _player = new MediaPlayer(this, this._logger);
            _player.PlayerStarted += OnPlaybackStarted;
            _player.PlayerStopped += OnPlaybackStopped;
            _player.PlayerEnded += OnPlaybackEnded;

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
            if (_movie.CanResume)
            {
                // show a resume dialog if move is resumable
                TimeSpan timespan = TimeSpan.FromTicks(_movie.ResumePositionTicks);
                string sbody = _movie.Name + "\n" + MediaBrowserPlugin.UI.Resource.ResumeFrom + " " + timespan.ToString();
                if (GUIUtils.ShowYesNoDialog(MediaBrowserPlugin.UI.Resource.ResumeFromLast, sbody, true))
                {
                    Play((int) timespan.TotalSeconds);
                }
            }

            Play();
        }

        #endregion       

        protected void Play(int resumeTime = 0)
        {
            var info = new MediaPlayerInfo
            {
                Title = _movie.Name,
                Year = _movie.ProductionYear.ToString(),
                Plot = _movie.Overview,
                Genre = _movie.Genres.FirstOrDefault(),
                Thumb = _cover.Filename,
                ResumePlaybackPosition = resumeTime
            };

            info.MediaFiles.Add(_movie.Path);
            _player.Play(info);
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
                GUITask.MainThreadCallback(() => Play(Parameters.ResumeFrom));
            }
        }

        protected void OnPlaybackStarted(MediaPlayerInfo info)
        {
            GUIContext.Instance.Client.ReportPlaybackStart(_movie.Id, GUIContext.Instance.ActiveUser.Id, PlaybackReported);
        }

        protected void OnPlaybackStopped(MediaPlayerInfo media, int progress)
        {
            GUIContext.Instance.Client.ReportPlaybackStopped(_movie.Id, GUIContext.Instance.ActiveUser.Id,
                TimeSpan.FromSeconds(progress).Ticks, PlaybackReported);
        }

        protected void OnPlaybackEnded(MediaPlayerInfo media)
        {
            // todo: doesn't account for multiparts!
            GUIContext.Instance.Client.ReportPlaybackStopped(_movie.Id, GUIContext.Instance.ActiveUser.Id,
                _movie.RunTimeTicks, PlaybackReported);
        }

        protected void PlaybackReported(bool response)
        {
            Log.Debug("Reporting playback state to MediaBrowser. {0}", response);
        }
    }
}
