/*
*  This file is part of pdn-content-aware-fill, A Resynthesizer-based
*  content aware fill Effect plug-in for Paint.NET.
*
*  Copyright (C) 2018, 2020, 2021, 2022 Nicholas Hayes
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
using System;
using System.Reflection;

namespace ContentAwareFill
{
    internal static class PluginIconUtil
    {
        private static readonly bool HighDpiIconsSupported;
        private static readonly MethodInfo CurrentScaleFactorMethodInfo;
        private static readonly MethodInfo DpiMethodInfo;
        private static readonly Pair<int, string>[] AvailableIcons;

        static PluginIconUtil()
        {
            HighDpiIconsSupported = typeof(ColorBgra).Assembly.GetName().Version >= new Version(4, 106);

            // Support for high-DPI Effect icons was added in Paint.NET version 4.1.6.
            if (HighDpiIconsSupported)
            {
                Type uiScaleFactorType = Type.GetType("PaintDotNet.UIScaleFactor, PaintDotNet.Core");

                if (uiScaleFactorType != null)
                {
                    PropertyInfo currentProperty = uiScaleFactorType.GetProperty("Current", BindingFlags.Public | BindingFlags.Static);
                    PropertyInfo dpiProperty = uiScaleFactorType.GetProperty("Dpi", BindingFlags.Public | BindingFlags.Instance);

                    if (currentProperty != null && dpiProperty != null)
                    {
                        CurrentScaleFactorMethodInfo = currentProperty.GetGetMethod();
                        DpiMethodInfo = dpiProperty.GetGetMethod();
                    }
                }
            }

            AvailableIcons = new Pair<int, string>[]
            {
                Pair.Create(96, "Resources.Icons.ContentAwareFill-96.png"),
                Pair.Create(192, "Resources.Icons.ContentAwareFill-192.png"),
                Pair.Create(384, "Resources.Icons.ContentAwareFill-384.png"),
            };
        }

        public static string GetIconResourceForCurrentDpi()
        {
            if (HighDpiIconsSupported && CurrentScaleFactorMethodInfo != null && DpiMethodInfo != null)
            {
                object currentScaleFactor = CurrentScaleFactorMethodInfo.Invoke(null, null);

                int currentDpi = (int)DpiMethodInfo.Invoke(currentScaleFactor, null);

                for (int i = 0; i < AvailableIcons.Length; i++)
                {
                    Pair<int, string> icon = AvailableIcons[i];

                    if (icon.First >= currentDpi)
                    {
                        return icon.Second;
                    }
                }

                return "Resources.Icons.ContentAwareFill-384.png";
            }
            else
            {
                return "Resources.Icons.ContentAwareFill-96.png";
            }
        }
    }
}
