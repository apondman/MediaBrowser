using System;
using System.Runtime.Serialization;
using MediaPortal.GUI.Library;
using System.Collections.Generic;
using MediaBrowser.Model.Querying;

namespace Pondman.MediaPortal.MediaBrowser
{
    public class MediaBrowserContextSettings : IEquatable<MediaBrowserContextSettings>
    {
        public MediaBrowserContextSettings(string contextId)
        {
            Context = contextId;
            Filters = new HashSet<ItemFilter>();
        }

        public string Context { get; set; }

        public GUIFacadeControl.Layout? Layout { get; set; }

        public HashSet<ItemFilter> Filters { get; set; }

        public bool Equals(MediaBrowserContextSettings other)
        {
            return this.Context == other.Context;
        }
    }
}