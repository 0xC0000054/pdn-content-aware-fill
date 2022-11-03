﻿/*
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

using System;
using System.Collections.Generic;
using System.Drawing;

namespace ContentAwareFill
{
    internal static class PointCollectionUtil
    {
        public static Rectangle GetBounds(IEnumerable<Point> points)
        {
            int left = int.MaxValue;
            int top = int.MaxValue;
            int right = 0;
            int bottom = 0;

            foreach (Point item in points)
            {
                left = Math.Min(left, item.X);
                top = Math.Min(top, item.Y);
                right = Math.Max(right, item.X);
                bottom = Math.Max(bottom, item.Y);
            }

            return Rectangle.FromLTRB(left, top, right, bottom);
        }

        public static Point GetCenter(IEnumerable<Point> points)
        {
            Rectangle bounds = GetBounds(points);

            int centerX = ((bounds.Right - bounds.Left) / 2) + bounds.Left;
            int centerY = ((bounds.Bottom - bounds.Top) / 2) + bounds.Top;

            return new Point(centerX, centerY);
        }
    }
}
