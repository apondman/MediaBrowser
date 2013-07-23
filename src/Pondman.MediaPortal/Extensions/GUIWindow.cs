using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaPortal.GUI.Library
{
    public static class GUIWindowExtensions
    {
        /// <summary>
        /// Focuses the specified control for this window.
        /// </summary>
        /// <param name="window">The window.</param>
        /// <param name="control">The control.</param>
        public static void Focus(this GUIWindow window, GUIControl control) 
        {
            control.Focus(window);
        }

        /// <summary>
        /// Determines whether this window is active.
        /// </summary>
        /// <param name="window">The window.</param>
        /// <returns>
        ///   <c>true</c> if the window is active; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsActive(this GUIWindow window)
        {
            return (window.GetID == GUIWindowManager.ActiveWindow);
        }
        
    }
}
