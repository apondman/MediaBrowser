using MediaBrowser.Model.Dto;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Session;
using MediaPortal.GUI.Library;
using Pondman.MediaPortal.MediaBrowser.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        private async void OnPlayerProgress(TimeSpan timeSpan)
        {
            await GUIContext.Instance.Client.ReportPlaybackProgressAsync(new PlaybackProgressInfo { ItemId = _movie.Id, PositionTicks = timeSpan.Ticks });
            Log.Debug("PlayerProgress: {0}", timeSpan.TotalSeconds);
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

        protected async void OnWindowStart()
        {
            if (String.IsNullOrEmpty(Parameters.Id))
            {
                if (_movie != null)
                {
                    await PublishItemDetails(_movie);
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
                Publish(".Loading", "True");
                GUIWaitCursor.Init();
                GUIWaitCursor.Show();
                var item = await LoadMovieDetails();
                await PublishItemDetails(item);
                Publish(".Loading", "False");
                GUIWaitCursor.Hide();
                if (Parameters.Playback)
                {
                    Parameters.Playback = false;
                    Play(Parameters.ResumeFrom);
                }
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
            if (_movie.IsPlaceHolder.HasValue && _movie.IsPlaceHolder.Value)
            {
                GUIUtils.ShowOKDialog("Please insert the following disc:", _movie.Name);
                return;
            }  
            
            var info = new MediaPlayerInfo
            {
                Title = _movie.Name,
                Year = _movie.ProductionYear.ToString(),
                Plot = _movie.Overview,
                Genre = _movie.Genres.FirstOrDefault(),
                ResumePlaybackPosition = resumeTime
            };

            var controls = GetSmartImageControls(_movie);
            foreach(var control in controls) 
            {
                info.Thumb = control.Resource.Filename;
            }

            var sources = _movie.MediaSources.ToList();
            foreach(var source in sources) 
            {
                var path = source.Path;

                if (String.IsNullOrWhiteSpace(path))
                {
                    path = GUIContext.Instance.Client.GetHlsVideoStreamUrl(new VideoStreamOptions { ItemId = source.Id });
                }

                info.MediaFiles.Add(path);
            }

            GUITask.MainThreadCallback(() => _player.Play(info));
        }

        protected async Task<BaseItemDto> LoadMovieDetails()
        {
            Log.Debug("Loading movie details for: {0}", Parameters.Id);
            try
            {
                _movie = await GUIContext.Instance.Client.GetItemAsync(Parameters.Id, GUIContext.Instance.Client.CurrentUserId);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }

            return _movie;
        }

        protected async void OnPlaybackStarted(MediaPlayerInfo info)
        {
            await GUIContext.Instance.Client.ReportPlaybackStartAsync( new PlaybackStartInfo { ItemId = _movie.Id, MediaSourceId = _movie.MediaSources[info.MediaFileIndex].Id });
            Log.Debug("Reporting playback started to MediaBrowser.");
        }

        protected async void OnPlaybackStopped(MediaPlayerInfo media, int progress)
        {
            await GUIContext.Instance.Client.ReportPlaybackStoppedAsync(new PlaybackStopInfo { ItemId = _movie.Id, PositionTicks = TimeSpan.FromSeconds(progress).Ticks, MediaSourceId = _movie.MediaSources[media.MediaFileIndex].Id });
            Log.Debug("Reporting playback stopped to MediaBrowser.");

            await GUIContext.Instance.Client.StopTranscodingProcesses(GUIContext.Instance.Client.DeviceId);
        }

        protected async void OnPlaybackEnded(MediaPlayerInfo media)
        {
            await GUIContext.Instance.Client.ReportPlaybackStoppedAsync(new PlaybackStopInfo { ItemId = _movie.Id, PositionTicks = _movie.MediaSources[media.MediaFileIndex].RunTimeTicks, MediaSourceId = _movie.MediaSources[media.MediaFileIndex].Id });
            Log.Debug("Reporting playback stopped to MediaBrowser.");

            await GUIContext.Instance.Client.StopTranscodingProcesses(GUIContext.Instance.Client.DeviceId);
        }

    }
}
