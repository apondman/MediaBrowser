using System;
using System.Linq;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaPortal.GUI.Library;

namespace Pondman.MediaPortal.MediaBrowser.GUI
{
    public class SmartImageControl
    {
        private readonly GUIImage _control;

        public SmartImageControl(GUIImage control)
        {
            var tokens = control.Description.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);

            Name = tokens[2];
            ImageType = ImageType.Primary;
            _control = control;

            Resource = new AsyncImageResource(MediaBrowserPlugin.Log)
            {
                Property = control.FileName,
                Delay = 0,
            };
        }

        public string Name { get; private set; }

        public ImageType ImageType { get; private set; }

        public int Width 
        {
            get
            {
                return _control.Width;
            }
        }

        public int Height 
        {
            get
            {
                return _control.Height;
            }
        }

        public AsyncImageResource Resource { get; private set; }

        public string GetImageUrl(BaseItemDto item)
        {
            return (item.ImageTags == null || item.ImageTags.Count == 0) ? string.Empty : GUIContext.Instance.Client.GetLocalImageUrl(item, new ImageOptions { ImageType = ImageType, Width = Width, Height = Height });
        }

        public static implicit operator SmartImageControl(GUIImage control)
        {
            return new SmartImageControl(control);
        }
    }
}