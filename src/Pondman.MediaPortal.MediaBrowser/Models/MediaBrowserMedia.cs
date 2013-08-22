namespace Pondman.MediaPortal.MediaBrowser.Models
{
    public class MediaBrowserMedia : MediaBrowserItem
    {
        public virtual int ResumeFrom { get; set; }

        public virtual bool Playback { get; set; }

        public static MediaBrowserMedia Browse(string id)
        {
            return new MediaBrowserMedia { Id = id };
        }

        public static MediaBrowserMedia Play(string id)
        {
            return new MediaBrowserMedia { Id = id, Playback = true };
        }

        public static MediaBrowserMedia Play(string id, int resumeFrom)
        {
            return new MediaBrowserMedia { Id = id, Playback = true, ResumeFrom = resumeFrom };
        }
    }
}
