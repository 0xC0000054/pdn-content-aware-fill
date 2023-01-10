/*
*  This file is part of pdn-content-aware-fill, A Resynthesizer-based
*  content aware fill Effect plug-in for Paint.NET.
*
*  Copyright (C) 2018, 2020, 2021, 2022, 2023 Nicholas Hayes
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

using System;

namespace ContentAwareFill
{
    internal static class PluginIconUtil
    {
        private static readonly ValueTuple<int, string>[] AvailableIcons;

        static PluginIconUtil()
        {
            AvailableIcons = new ValueTuple<int, string>[]
            {
                ValueTuple.Create(96, "Resources.Icons.ContentAwareFill-96.png"),
                ValueTuple.Create(120, "Resources.Icons.ContentAwareFill-120.png"),
                ValueTuple.Create(144, "Resources.Icons.ContentAwareFill-144.png"),
                ValueTuple.Create(192, "Resources.Icons.ContentAwareFill-192.png"),
                ValueTuple.Create(384, "Resources.Icons.ContentAwareFill-384.png"),
            };
        }

        public static string GetIconResourceForDpi(int dpi)
        {
            for (int i = 0; i < AvailableIcons.Length; i++)
            {
                ValueTuple<int, string> icon = AvailableIcons[i];

                if (icon.Item1 >= dpi)
                {
                    return icon.Item2;
                }
            }

            return "Resources.Icons.ContentAwareFill-384.png";
        }
    }
}
