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

using System.Collections.Generic;
using System.Drawing;

namespace ContentAwareFill
{
    internal abstract class PointComparer : IComparer<Point>
    {
        private static readonly CartesianPointComparer lessCartesian = new CartesianPointComparer(false);
        private static readonly CartesianPointComparer moreCartesian = new CartesianPointComparer(true);
        private static readonly HorizontalPointComparer inwardHorizontal = new HorizontalPointComparer(false);
        private static readonly HorizontalPointComparer outwardHorizontal = new HorizontalPointComparer(true);
        private static readonly VerticalPointComparer inwardVertical = new VerticalPointComparer(false);
        private static readonly VerticalPointComparer outwardVertical = new VerticalPointComparer(true);

        internal static PointComparer LessCartesian
        {
            get
            {
                return lessCartesian;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
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

        internal static PointComparer CreateInwardConcentric(IEnumerable<Point> points)
        {
            return new DirectionalPointComparer(points, false);
        }

        internal static PointComparer CreateOutwardConcentric(IEnumerable<Point> points)
        {
            return new DirectionalPointComparer(points, true);
        }

#pragma warning disable RCS1168 // Parameter name differs from base name.
        public abstract int Compare(Point point1, Point point2);
#pragma warning restore RCS1168 // Parameter name differs from base name.
    }
}
