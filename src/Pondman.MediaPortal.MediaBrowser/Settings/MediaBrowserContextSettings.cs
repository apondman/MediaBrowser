using System;
using System.Runtime.Serialization;
using MediaPortal.GUI.Library;
using System.Collections.Generic;
using MediaBrowser.Model.Querying;

namespace Pondman.MediaPortal.MediaBrowser
{
    [DataContract(Name = "ContextSettings", Namespace = "urn://mediaportal/mb3/settings/context")]
    public class MediaBrowserContextSettings : IEquatable<MediaBrowserContextSettings>
    {
        public MediaBrowserContextSettings(string contextId)
        {
            Context = contextId;
            Filters = new HashSet<ItemFilter>();
        }

        [DataMember]
        public string Context { get; private set; }

        [DataMember]
        public GUIFacadeControl.Layout? Layout { get; set; }

        [DataMember]
        public HashSet<ItemFilter> Filters { get; set; }

        public bool Equals(MediaBrowserContextSettings other)
        {
            return this.Context == other.Context;
        }
    }
}