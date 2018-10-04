/*
*  This file is part of pdn-content-aware-fill, A Resynthesizer-based
*  content aware fill Effect plug-in for Paint.NET.
*
*  Copyright (C) 2018 Nicholas Hayes
*
*  This program is free software; you can redistribute it and/or modify
*  it under the terms of the GNU General Public License as published by
*  the Free Software Foundation; either version 2 of the License, or
*  (at your option) any later version.
*
*  This program is distributed in the hope that it will be useful,
*  but WITHOUT ANY WARRANTY; without even the implied warranty of
*  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*  GNU General Public License for more details.
*
*  You should have received a copy of the GNU General Public License
*  along with this program; if not, write to the Free Software
*  Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
*
*/

using PaintDotNet;
using PaintDotNet.Effects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace ContentAwareFill
{
    internal static class PluginThemingUtil
    {
        private static Action<EffectConfigDialog, bool> useAppThemeSetter;
        private static bool initAppThemeSetter = false;

        /// <summary>
        /// Enables theming for the specified dialog.
        /// </summary>
        /// <param name="dialog">The dialog.</param>
        /// <exception cref="ArgumentNullException"><paramref name="dialog"/> is null.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public static void EnableEffectDialogTheme(EffectConfigDialog dialog)
        {
            if (dialog == null)
            {
                throw new ArgumentNullException(nameof(dialog));
            }

            try
            {
                if (!initAppThemeSetter)
                {
                    initAppThemeSetter = true;

                    PropertyInfo propertyInfo = typeof(EffectConfigDialog).GetProperty("UseAppThemeColors");
                    if (propertyInfo != null)
                    {
                        useAppThemeSetter = (Action<EffectConfigDialog, bool>)Delegate.CreateDelegate(typeof(Action<EffectConfigDialog, bool>), propertyInfo.GetSetMethod());
                    }
                }

                useAppThemeSetter?.Invoke(dialog, true);
            }
            catch
            {
                // Ignore any exceptions that are thrown when trying to enable the dialog theming.
                // The dialog should be shown to the user even if theming could not be enabled.
            }
        }

        /// <summary>
        /// Updates the background color of the controls.
        /// </summary>
        /// <param name="root">The root control.</param>
        /// <exception cref="ArgumentNullException"><paramref name="root"/> is null.</exception>
        public static void UpdateControlBackColor(Control root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            Color backColor = root.BackColor;

            Stack<Control> stack = new Stack<Control>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                Control parent = stack.Pop();

                Control.ControlCollection controls = parent.Controls;

                for (int i = 0; i < controls.Count; i++)
                {
                    Control control = controls[i];

                    if (control is Button button)
                    {
                        // Reset the BackColor of all Button controls.
                        button.UseVisualStyleBackColor = true;
                    }
                    else
                    {
                        // Update the BackColor for all child controls as some controls
                        // do not change the BackColor when the parent control does.

                        control.BackColor = backColor;

                        if (control.HasChildren)
                        {
                            stack.Push(control);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates the foreground color of the controls.
        /// </summary>
        /// <param name="root">The root control.</param>
        /// <exception cref="ArgumentNullException"><paramref name="root"/> is null.</exception>
        public static void UpdateControlForeColor(Control root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            Color foreColor = root.ForeColor;

            Stack<Control> stack = new Stack<Control>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                Control parent = stack.Pop();

                Control.ControlCollection controls = parent.Controls;

                for (int i = 0; i < controls.Count; i++)
                {
                    Control control = controls[i];

                    if (control is Button button)
                    {
                        // Reset the ForeColor of all Button controls.
                        button.ForeColor = SystemColors.ControlText;
                    }
                    else if (control is LinkLabel link)
                    {
                        if (foreColor != Control.DefaultForeColor)
                        {
                            link.LinkColor = foreColor;
                        }
                        else
                        {
                            // If the control is using the default foreground color set the link color
                            // to Color.Empty so the LinkLabel will use its default colors.
                            link.LinkColor = Color.Empty;
                        }
                    }
                    else
                    {
                        // Update the ForeColor for all child controls as some controls
                        // do not change the ForeColor when the parent control does.

                        control.ForeColor = foreColor;

                        if (control.HasChildren)
                        {
                            stack.Push(control);
                        }
                    }
                }
            }
        }
    }
}
