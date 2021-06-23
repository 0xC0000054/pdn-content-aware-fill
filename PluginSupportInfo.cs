/*
*  This file is part of pdn-content-aware-fill, A Resynthesizer-based
*  content aware fill Effect plug-in for Paint.NET.
*
*  Copyright (C) 2018, 2020, 2021 Nicholas Hayes
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
using System.Reflection;
using PaintDotNet;

namespace ContentAwareFill
{
    public sealed class PluginSupportInfo : IPluginSupportInfo
    {
        public string Author
        {
            get
            {
                return ((AssemblyCompanyAttribute)typeof(ContentAwareFillEffect).Assembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false)[0]).Company;
            }
        }

        public string Copyright
        {
            get
            {
                return ((AssemblyCopyrightAttribute)typeof(ContentAwareFillEffect).Assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false)[0]).Copyright;
            }
        }

        public string DisplayName
        {
            get
            {
                return ContentAwareFillEffect.StaticName;
            }
        }

        public Version Version
        {
            get
            {
                return typeof(ContentAwareFillEffect).Assembly.GetName().Version;
            }
        }

        public Uri WebsiteUri
        {
            get
            {
                return new Uri("https://forums.getpaint.net/index.php?showtopic=112730");
            }
        }
    }
}
