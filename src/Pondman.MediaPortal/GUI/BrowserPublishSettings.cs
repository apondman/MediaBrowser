namespace Pondman.MediaPortal.GUI
{
    public class BrowserPublishSettings
    {
        public BrowserPublishSettings()
        {
            Prefix = "#";
            Delay = 250;
            LoadingPlaceholderLabel = "Loading ...";
        }
        
        public string Prefix { get; set; }
        
        public int Delay { get; set; }

        public int Limit { get; set; }

        public string LoadingPlaceholderLabel { get; set; }

    }
}