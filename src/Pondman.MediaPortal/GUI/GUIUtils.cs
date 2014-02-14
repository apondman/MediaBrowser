using MediaPortal.Dialogs;
using MediaPortal.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MediaPortal.GUI.Library
{
    /// <summary>
    /// Collection of common GUI utility methods
    /// </summary>
    public static class GUIUtils
    {
        static CultureInfo _cultureInfo;
        static HashSet<string> _registeredProperties;
        static readonly object lockObject = new object();

        static GUIUtils() 
        {
            _cultureInfo = CultureInfo.CreateSpecificCulture(GUILocalizeStrings.GetCultureName(GUILocalizeStrings.CurrentLanguage()));
            _registeredProperties = new HashSet<string>();
            
            // setup property management
            GUIPropertyManager.OnPropertyChanged += RegisterProperty;
        }

        public static CultureInfo Culture
        {
            get
            {
                return _cultureInfo;
            }
        }

        static void RegisterProperty(string tag, string tagValue)
        {
            lock (lockObject)
            {
                _registeredProperties.Add(tag);
            }
        }

        /// <summary>
        /// Gets the property with the given tag.
        /// </summary>
        /// <param name="tag">the tag of the property</param>
        /// <returns>the value of the property</returns>
        public static string Read(string tag)
        {
            string value = GUIPropertyManager.GetProperty(tag);
            return value ?? string.Empty;
        }

        /// <summary>
        /// Sets the property, or when the value is omitted reset the property.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <param name="value">The value</param>
        public static void Publish(string tag, string value = null)
        {
            if (string.IsNullOrEmpty(value))
            {
                value = " ";
            }
            
            GUIPropertyManager.SetProperty(tag, value);
        }

        /// <summary>
        /// Publishes the properties for the given object to the skin engine.
        /// </summary>
        /// <param name="obj">instance of an object to publish.</param>
        /// <param name="tag">the skin property tag.</param>
        /// <param name="exclude">a list of property names that should be excluded from being published</param>
        public static void Publish(this object obj, string tag = "#", params string[] exclude)
        {
            string[] filter = exclude ?? new string[] {};
            Type objType = obj != null ? obj.GetType() : null;
            if (obj == null || obj is string || objType.IsPrimitive || obj is Guid)
            {           
                if (obj is float || obj is double || obj is decimal)
                {
                    float value = Convert.ToSingle(obj);

                    Publish(tag + ".Localized", value.ToString(_cultureInfo));
                    Publish(tag + ".Invariant", value.ToString(CultureInfo.InvariantCulture));
                }
                else if (obj != null)
                {
                    string value = obj.ToString().Trim();
                    Publish(tag, value);
                }
                else
                {
                    Unpublish(tag);
                }
            }
            else if (obj is IEnumerable)
            {
                // check if it's a generic dictionary
                bool keys = false;
                if (objType.IsGenericType)
                {
                    Type baseType = objType.GetGenericTypeDefinition();
                    if (baseType == typeof(Dictionary<,>))
                    {
                        keys = true;
                    }
                }

                int i = 0;

                foreach (var item in (IEnumerable)obj)
                {
                    if (keys)
                    {
                        Type itemType = item.GetType();
                        object key = itemType.GetProperty("Key").GetValue(item, null);
                        object value = itemType.GetProperty("Value").GetValue(item, null);
                        
                        Publish(value, tag + "." + key.ToString());
                    }
                    else
                    {
                        Publish(item, tag + "." + i);
                    }
                    i++;
                }

                Publish(i, tag + ".Count");
            }
            else
            {
                if (obj is DateTime)
                {
                    var dt = (DateTime)obj;
                    filter = new string[] { "Date", "DayOfWeek" };

                    // todo: more patterns? (overkill?) / configurable custom date format through settings?
                    // reference: http://msdn.microsoft.com/en-us/library/8kb3ddd4.aspx

                    Publish(tag + ".MonthName", dt.ToString("MMMM", _cultureInfo));
                    Publish(tag + ".DayOfWeek", dt.ToString("dddd", _cultureInfo));
                    Publish(tag + ".ShortDate", dt.ToString(_cultureInfo.DateTimeFormat.ShortDatePattern));
                    Publish(tag + ".LongDate", dt.ToString(_cultureInfo.DateTimeFormat.LongDatePattern));
                    string timestamp = dt.ToString("yyyyMMddHHmmss");
                    Publish(tag + ".Timestamp.Long", timestamp);
                    Publish(tag + ".Timestamp.Date", timestamp.Substring(0, 8));
                    Publish(tag + ".Timestamp.Time", timestamp.Substring(8, 6));               
                }
                
                var properties = objType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => !filter.Contains(p.Name));
                foreach (PropertyInfo property in properties)
                {
                    string name = tag + "." + property.Name;
                    object value = property.GetValue(obj, null);
                    Publish(value, name);
                }
            }
        }

        /// <summary>
        /// Resets the properties starting with the given tag.
        /// </summary>
        /// <param name="startsWithTag">The starts with tag.</param>
        public static void Unpublish(string startsWithTag = "#")
        {
            List<string> selection = null;
            lock (lockObject)
            {
                selection = _registeredProperties.Where(p => p.StartsWith(startsWithTag)).ToList();
            }

            // wipe exact tag
            GUIUtils.Publish(startsWithTag);

            // wipe all tags starting with the prefix that we know of
            selection.ForEach(p => GUIUtils.Publish(p));
        }

        #region Dialogs

        private delegate int ShowMenuDialogDelegate(string heading, List<GUIListItem> items, int selectedItemIndex);

        /// <summary>
        /// Displays a menu dialog from list of items
        /// </summary>
        /// <returns>Selected item index, -1 if exited</returns>
        public static int ShowMenuDialog(string heading, List<GUIListItem> items, int selectedItemIndex = -1)
        {
            if (GUIGraphicsContext.form.InvokeRequired)
            {
                ShowMenuDialogDelegate d = ShowMenuDialog;
                return (int)GUIGraphicsContext.form.Invoke(d, heading, items, selectedItemIndex);
            }

            GUIDialogMenu dlgMenu = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);

            dlgMenu.Reset();
            dlgMenu.SetHeading(heading);

            foreach (GUIListItem item in items)
            {
                dlgMenu.Add(item);
            }

            dlgMenu.SelectedLabel = selectedItemIndex;
            dlgMenu.DoModal(GUIWindowManager.ActiveWindow);

            if (dlgMenu.SelectedLabel < 0)
            {
                return -1;
            }

            return dlgMenu.SelectedLabel;
        }

        private delegate string ShowKeyboardDelegate(string heading, bool password);

        public static string ShowKeyboard(string heading, bool password = false)
        {
            if (GUIGraphicsContext.form.InvokeRequired)
            {
                ShowKeyboardDelegate d = ShowKeyboard;
                return (string)GUIGraphicsContext.form.Invoke(d, heading, password);
            }

            VirtualKeyboard keyboard = (VirtualKeyboard)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_VIRTUAL_KEYBOARD);
            if (null == keyboard)
            {
                return null;
            }

            keyboard.Reset();
            keyboard.Text = heading;
            keyboard.Password = password;
            keyboard.DoModal(GUIWindowManager.ActiveWindow);

            if (keyboard.IsConfirmed && keyboard.Text != heading)
            {
                return keyboard.Text;
            }

            return null;
        }

        private delegate void ShowTextDialogDelegate(string heading, string text);

        /// <summary>
        /// Displays a text dialog.
        /// </summary>
        public static void ShowTextDialog(string heading, List<string> text)
        {
            if (text == null || text.Count == 0) return;
            ShowTextDialog(heading, string.Join("\n", text.ToArray()));
        }

        /// <summary>
        /// Displays a text dialog.
        /// </summary>
        public static void ShowTextDialog(string heading, string text)
        {
            if (GUIGraphicsContext.form.InvokeRequired)
            {
                ShowTextDialogDelegate d = ShowTextDialog;
                GUIGraphicsContext.form.Invoke(d, heading, text);
                return;
            }

            GUIDialogText dlgText = (GUIDialogText)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_TEXT);

            dlgText.Reset();
            dlgText.SetHeading(heading);
            dlgText.SetText(text);

            dlgText.DoModal(GUIWindowManager.ActiveWindow);
        }

        private delegate void ShowOKDialogDelegate(string heading, string lines);

        /// <summary>
        /// Displays a OK dialog with heading and up to 4 lines.
        /// </summary>
        public static void ShowOKDialog(string heading, string line1, string line2, string line3, string line4)
        {
            ShowOKDialog(heading, string.Concat(line1, line2, line3, line4));
        }

        /// <summary>
        /// Displays a OK dialog with heading and up to 4 lines split by \n in lines string.
        /// </summary>
        public static void ShowOKDialog(string heading, string lines)
        {
            if (GUIGraphicsContext.form.InvokeRequired)
            {
                ShowOKDialogDelegate d = ShowOKDialog;
                GUIGraphicsContext.form.Invoke(d, heading, lines);
                return;
            }

            GUIDialogOK dlgOK = (GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);

            dlgOK.Reset();
            dlgOK.SetHeading(heading);

            int lineid = 1;
            foreach (string line in lines.Split(new string[] { "\\n", "\n" }, StringSplitOptions.None))
            {
                dlgOK.SetLine(lineid, line);
                lineid++;
            }
            for (int i = lineid; i <= 4; i++)
                dlgOK.SetLine(i, string.Empty);

            dlgOK.DoModal(GUIWindowManager.ActiveWindow);
        }

        private delegate bool ShowCustomYesNoDialogDelegate(string heading, string lines, string yesLabel, string noLabel, bool defaultYes);

        /// <summary>
        /// Displays a yes/no dialog.
        /// </summary>
        /// <returns>True if yes was clicked, False if no was clicked</returns>
        public static bool ShowYesNoDialog(string heading, string lines)
        {
            return ShowCustomYesNoDialog(heading, lines, null, null, false);
        }

        /// <summary>
        /// Displays a yes/no dialog.
        /// </summary>
        /// <returns>True if yes was clicked, False if no was clicked</returns>
        public static bool ShowYesNoDialog(string heading, string lines, bool defaultYes)
        {
            return ShowCustomYesNoDialog(heading, lines, null, null, defaultYes);
        }

        /// <summary>
        /// Displays a yes/no dialog with custom labels for the buttons.
        /// This method may become obsolete in the future if media portal adds more dialogs.
        /// </summary>
        /// <returns>True if yes was clicked, False if no was clicked</returns>
        public static bool ShowCustomYesNoDialog(string heading, string lines, string yesLabel, string noLabel)
        {
            return ShowCustomYesNoDialog(heading, lines, yesLabel, noLabel, false);
        }

        /// <summary>
        /// Displays a yes/no dialog with custom labels for the buttons.
        /// This method may become obsolete in the future if media portal adds more dialogs.
        /// </summary>
        /// <returns>True if yes was clicked, False if no was clicked</returns>
        public static bool ShowCustomYesNoDialog(string heading, string lines, string yesLabel, string noLabel, bool defaultYes)
        {
            if (GUIGraphicsContext.form.InvokeRequired)
            {
                ShowCustomYesNoDialogDelegate d = ShowCustomYesNoDialog;
                return (bool)GUIGraphicsContext.form.Invoke(d, heading, lines, yesLabel, noLabel, defaultYes);
            }

            GUIDialogYesNo dlgYesNo = (GUIDialogYesNo)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_YES_NO);

            try
            {
                dlgYesNo.Reset();
                dlgYesNo.SetHeading(heading);
                string[] linesArray = lines.Split(new string[] { "\\n", "\n" }, StringSplitOptions.None);
                if (linesArray.Length > 0) dlgYesNo.SetLine(1, linesArray[0]);
                if (linesArray.Length > 1) dlgYesNo.SetLine(2, linesArray[1]);
                if (linesArray.Length > 2) dlgYesNo.SetLine(3, linesArray[2]);
                if (linesArray.Length > 3) dlgYesNo.SetLine(4, linesArray[3]);
                dlgYesNo.SetDefaultToYes(defaultYes);

                foreach (GUIControl item in dlgYesNo.Children)
                {
                    if (item is GUIButtonControl)
                    {
                        GUIButtonControl btn = (GUIButtonControl)item;
                        if (btn.GetID == 11 && !string.IsNullOrEmpty(yesLabel)) // Yes button
                            btn.Label = yesLabel;
                        else if (btn.GetID == 10 && !string.IsNullOrEmpty(noLabel)) // No button
                            btn.Label = noLabel;
                    }
                }
                dlgYesNo.DoModal(GUIWindowManager.ActiveWindow);
                return dlgYesNo.IsConfirmed;
            }
            finally
            {
                // set the standard yes/no dialog back to it's original state (yes/no buttons)
                if (dlgYesNo != null)
                {
                    dlgYesNo.ClearAll();
                }
            }
        }

        #endregion

        public static Image LoadImage(string filepath)
        {
            if (string.IsNullOrEmpty(filepath) || !File.Exists(filepath)) return null;

            Image img = null;

            try
            {
                img = ImageFast.FromFile(filepath);
            }
            catch
            {
                try { img = Image.FromFile(filepath); }
                catch { }
            }

            return img;
        }

        public static string LoadTexture(string filepath)
        {
            var image = LoadImage(filepath);
            string texture = GetTextureIdentFromFile(filepath);
            
            if (GUITextureManager.LoadFromMemory(image, texture, 0, 0, 0) > 0) return texture;

            return null;
        }

        public static string GetTextureIdentFromFile(string filename, string suffix = "")
        {
            return "[Image:" + (filename + suffix).GetHashCode() + "]";
        }

        public static void LoadImageFromMemory(this GUIListItem item, string filepath) 
        {
            var texture = LoadTexture(filepath);
            if (texture == null) return;

            item.ThumbnailImage = texture;
            item.IconImage = texture;
            item.IconImageBig = texture;

            // todo: signal refresh?
        }
            

    }
}
