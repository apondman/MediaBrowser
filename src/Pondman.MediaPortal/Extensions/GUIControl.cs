using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaPortal.GUI.Library
{
    public static class GUIControlExtensions
    {
        /// <summary>
        /// Focuses the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="window">Optional window to focus in, if not specified defaults to ActiveWindow.</param>
        public static void Focus(this GUIControl control, GUIWindow window = null)
        {
            GUIControl.FocusControl((window != null) ? window.GetID : GUIWindowManager.ActiveWindow, control.GetID);
        } 

    }
}
