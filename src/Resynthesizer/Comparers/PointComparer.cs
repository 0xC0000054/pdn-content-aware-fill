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

using PaintDotNet.Rendering;
using System.Collections.Generic;

namespace ContentAwareFill
{
    internal abstract class PointComparer : IComparer<Point2Int32>
    {
        private static readonly CartesianPointComparer lessCartesian = new(false);
        private static readonly CartesianPointComparer moreCartesian = new(true);
        private static readonly HorizontalPointComparer inwardHorizontal = new(false);
        private static readonly HorizontalPointComparer outwardHorizontal = new(true);
        private static readonly VerticalPointComparer inwardVertical = new(false);
        private static readonly VerticalPointComparer outwardVertical = new(true);

        internal static PointComparer LessCartesian
        {
            get
            {
                return lessCartesian;
            }
        }

        internal static PointComparer MoreCartesian
        {
            get
            {
                return moreCartesian;
            }
        }

        internal static PointComparer InwardHorizontal
        {
            get
            {
                return inwardHorizontal;
            }
        }

        internal static PointComparer OutwardHorizontal
        {
            get
            {
                return outwardHorizontal;
            }
        }

        internal static PointComparer InwardVertical
        {
            get
            {
                return inwardVertical;
            }
        }

        internal static PointComparer OutwardVertical
        {
            get
            {
                return outwardVertical;
            }
        }

        internal static PointComparer CreateInwardConcentric(IEnumerable<Point2Int32> points)
        {
            return new DirectionalPointComparer(points, false);
        }

        internal static PointComparer CreateOutwardConcentric(IEnumerable<Point2Int32> points)
        {
            return new DirectionalPointComparer(points, true);
        }

        public abstract int Compare(Point2Int32 point1, Point2Int32 point2);
    }
}
