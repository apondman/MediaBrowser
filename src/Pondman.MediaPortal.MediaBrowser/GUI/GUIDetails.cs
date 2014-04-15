﻿using MediaBrowser.Model.Dto;
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
            await GUIContext.Instance.Client.ReportPlaybackProgressAsync(new PlaybackProgressInfo { ItemId = _movie.Id, UserId = GUIContext.Instance.ActiveUser.Id, PositionTicks = timeSpan.Ticks });
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
                var item = await LoadMovieDetails();
                await PublishItemDetails(item);
                Publish(".Loading", "False");

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

            SmartImageControl resource;
            if (_smartImageControls.TryGetValue(_movie.Type, out resource))
            {
                // load specific image
                info.Thumb = resource.Resource.Filename;
            }

            // load default image
            if (_smartImageControls.TryGetValue("Default", out resource))
            {
                info.Thumb = resource.Resource.Filename;
            }

            info.MediaFiles.Add(_movie.Path);
            GUITask.MainThreadCallback(() => _player.Play(info));
        }

        protected async Task<BaseItemDto> LoadMovieDetails()
        {
            // todo: update this logic with true async 

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
            await GUIContext.Instance.Client.ReportPlaybackStartAsync( new PlaybackStartInfo { ItemId = _movie.Id, UserId = GUIContext.Instance.ActiveUser.Id, IsSeekable = true });
            Log.Debug("Reporting playback started to MediaBrowser.");
        }

        protected async void OnPlaybackStopped(MediaPlayerInfo media, int progress)
        {
            await GUIContext.Instance.Client.ReportPlaybackStoppedAsync(new PlaybackStopInfo { ItemId = _movie.Id, UserId = GUIContext.Instance.ActiveUser.Id, PositionTicks = TimeSpan.FromSeconds(progress).Ticks });
            Log.Debug("Reporting playback stopped to MediaBrowser.");
        }

        protected async void OnPlaybackEnded(MediaPlayerInfo media)
        {
            // todo: doesn't account for multiparts!
            await GUIContext.Instance.Client.ReportPlaybackStoppedAsync(new PlaybackStopInfo { ItemId = _movie.Id, UserId = GUIContext.Instance.ActiveUser.Id, PositionTicks = _movie.RunTimeTicks });
            Log.Debug("Reporting playback stopped to MediaBrowser.");
        }

    }
}
