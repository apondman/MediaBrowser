using MediaPortal.GUI.Library;
using MediaPortal.Player;
using System;
using System.Threading;

namespace Pondman.MediaPortal
{
    public enum MediaPlayerState 
    { 
        Idle, 
        Processing, 
        Playing 
    }
    
    public class MediaPlayer
    {
        #region Variables
        
        protected GUIWindow _window;
        protected ILogger _logger;
        protected MediaPlayerState _state;

        #endregion

        public event Action<MediaPlayerInfo> PlayerStarted;

        #region Ctor

        public MediaPlayer(GUIWindow window, ILogger logger = null)
        {
            Guard.NotNull(() => window, window);
            _logger = logger ?? NullLogger.Instance;
            
            // hookup internal playback handlers
            g_Player.PlayBackStarted += new g_Player.StartedHandler(OnPlaybackStarted);
            g_Player.PlayBackEnded += new g_Player.EndedHandler(OnPlayBackEnded);
            g_Player.PlayBackStopped += new g_Player.StoppedHandler(OnPlayBackStoppedOrChanged);
            g_Player.PlayBackChanged += new g_Player.ChangedHandler(OnPlayBackStoppedOrChanged);
        }

        ~MediaPlayer()
        {
            // unhook internal playback handlers
            g_Player.PlayBackStarted -= new g_Player.StartedHandler(OnPlaybackStarted);
            g_Player.PlayBackEnded -= new g_Player.EndedHandler(OnPlayBackEnded);
            g_Player.PlayBackStopped -= new g_Player.StoppedHandler(OnPlayBackStoppedOrChanged);
            g_Player.PlayBackChanged -= new g_Player.ChangedHandler(OnPlayBackStoppedOrChanged);
        }

        #endregion

        #region Protected Properties

        protected ILogger Log
        {
            get
            {
                return _logger;
            }
        }

        #endregion

        #region Internal Event Handlers

        protected void OnPlaybackStarted(g_Player.MediaType type, string filename)
        {
            g_Player.ShowFullScreenWindow();

            MediaPlayerInfo info = new MediaPlayerInfo(filename);
            if (PlayerStarted != null)
            {
                PlayerStarted(info);
                UpdateOSD(info);
            }
        }

        protected void OnPlayBackStoppedOrChanged(g_Player.MediaType type, int timeMovieStopped, string filename)
        {
            Reset();
        }

        protected void OnPlayBackEnded(g_Player.MediaType type, string filename)
        {
            Reset();
        }

        #endregion

        public virtual void Play(string path)
        {
            // Play the file using the mediaportal player
            _logger.Debug("Play: Path={0}", path);           
            
            bool success = g_Player.Play(path.Trim(), g_Player.MediaType.Video);

            // if the playback started and we are still playing go full screen (internal player)
            if (!success)
            {
                _logger.Error("Playback failed: Media={0}", path);
                Reset();
            }
        }

        public virtual void Stop()
        {
            if (g_Player.Playing)
            {
                g_Player.Stop();
            }
        }

        public bool IsPlaying
        {
            get
            {
                return (_state != MediaPlayerState.Idle);
            }
        }

        protected void Reset() 
        {
            _state = MediaPlayerState.Idle;
        }

        protected void UpdateOSD(MediaPlayerInfo info)
        {
            Timer delayed = new Timer((x) => 
            {
                GUIPropertyManager.SetProperty("#Play.Current.Title", info.Title);
                GUIPropertyManager.SetProperty("#Play.Current.Plot", info.Plot);
                GUIPropertyManager.SetProperty("#Play.Current.Thumb", info.Thumb);
                GUIPropertyManager.SetProperty("#Play.Current.Year", info.Year);
                GUIPropertyManager.SetProperty("#Play.Current.Genre", info.Genre);
            }, null, 2000, -1);
        }
    }
}
