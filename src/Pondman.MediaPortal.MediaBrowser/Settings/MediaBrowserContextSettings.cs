using System;
using System.Runtime.Serialization;
using MediaPortal.GUI.Library;

namespace Pondman.MediaPortal.MediaBrowser
{
    [DataContract(Name = "ContextSettings", Namespace = "http://mediabrowser3.com/settings/context")]
    public class MediaBrowserContextSettings : IEquatable<MediaBrowserContextSettings>
    {
        public MediaBrowserContextSettings(string contextId)
        {
            Context = contextId;
        }

        [DataMember]
        public string Context { get; private set; }

        [DataMember]
        public GUIFacadeControl.Layout? Layout { get; set; }

        public bool Equals(MediaBrowserContextSettings other)
        {
            return this.Context == other.Context;
        }
    }
}