using MediaBrowser.Model.Dto;
using MediaPortal.GUI.Library;
using Pondman.MediaPortal.MediaBrowser.Models;
using System;
using System.Linq;
using System.Threading;
using MPGui = MediaPortal.GUI.Library;

namespace Pondman.MediaPortal.MediaBrowser.GUI
{
    public class GUIDetails : GUIDefault<MediaBrowserMedia>
    {
        private BaseItemDto _movie = null;
        private readonly MediaPlayer _player = null;

        #region Constructors

        public GUIDetails() : base(MediaBrowserWindow.Details)
        {
            _player = new MediaPlayer(this, this._logger);
            _player.PlayerStarted += OnPlaybackStarted;
            _player.PlayerStopped += OnPlaybackStopped;
            _player.PlayerEnded += OnPlaybackEnded;
            _player.PlayerProgress += OnPlayerProgress;

            RegisterCommand("Play", PlayCommand);
        }

        private void OnPlayerProgress(TimeSpan timeSpan)
        {
            GUIContext.Instance.Client.ReportPlaybackProgress(_movie.Id, GUIContext.Instance.ActiveUser.Id,
                timeSpan.Ticks, false, false, x => Log.Debug("PlayerProgress: {0}", timeSpan.TotalSeconds));
        }

        #endregion

        protected override void OnPageLoad()
        {
            base.OnPageLoad();

            if (!GUIContext.Instance.IsServerReady || !GUIContext.Instance.Client.IsUserLoggedIn) return;
            OnWindowStart();
        }

        protected override void Reset()
        {
             OnWindowStart();
        }

        protected void OnWindowStart()
        {
            if (String.IsNullOrEmpty(Parameters.Id))
            {
                if (_movie != null)
                {
                    PublishItemDetails(_movie);
                }
                else
                {
                    GUIWindowManager.ShowPreviousWindow();
                }
            }
            else
            {
                // Clear details
                GUIUtils.Unpublish(_publishPrefix);
                
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
                var timespan = TimeSpan.FromTicks(_movie.ResumePositionTicks);
                var sbody = _movie.Name + "\n" + MediaBrowserPlugin.UI.Resource.ResumeFrom + " " + timespan.ToString();
                if (!GUIUtils.ShowYesNoDialog(MediaBrowserPlugin.UI.Resource.ResumeFromLast, sbody, true)) return;

                Play((int) timespan.TotalSeconds);
                return;
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
                ResumePlaybackPosition = resumeTime
            };

            SmartImageControl resource;
            if (ImageResources.TryGetValue(_movie.Type, out resource))
            {
                // load specific image
                info.Thumb = resource.Resource.Filename;
            }

            // load default image
            if (ImageResources.TryGetValue("Default", out resource))
            {
                info.Thumb = resource.Resource.Filename;
            }

            info.MediaFiles.Add(_movie.Path);
            _player.Play(info);
        }

        protected BaseItemDto LoadMovieDetails(GUITask task)
        {
            Log.Debug("Loading movie details for: {0}", Parameters.Id);
            
            Publish(".Loading", "True");
            var mre = new ManualResetEvent(false);

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
            Publish(".Loading", "False");

            return _movie;
        }

        protected void PublishMovieDetailsTask(BaseItemDto movie)
        {
            PublishItemDetails(movie);
            if (!Parameters.Playback) return;
            Parameters.Playback = false;
            GUITask.MainThreadCallback(() => Play(Parameters.ResumeFrom));
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
