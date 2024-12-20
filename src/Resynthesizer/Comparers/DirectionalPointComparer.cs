﻿/*
*  This file is part of pdn-content-aware-fill, A Resynthesizer-based
*  content aware fill Effect plug-in for Paint.NET.
*
*  Copyright (C) 2018, 2020, 2021, 2022, 2023, 2024 Nicholas Hayes
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
*
* This file incorporates work covered by the following copyright and
* permission notice:
*
* The Resynthesizer - A GIMP plug-in for resynthesizing textures
*
*  Copyright (C) 2010  Lloyd Konneker
*  Copyright (C) 2000 2008  Paul Francis Harrison
*  Copyright (C) 2002  Laurent Despeyroux
*  Copyright (C) 2002  David Rodríguez García
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
*  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
*
*/

using PaintDotNet.Rendering;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;

namespace ContentAwareFill
{
    internal struct DirectionalPointComparer : IComparer<Point2Int32>, IDisposable
    {
        private readonly uint[] maxCartesianAlongRay;
        private readonly bool outward;
        private int disposed;

        public DirectionalPointComparer(IEnumerable<Point2Int32> targetPoints, bool outward)
        {
            ArgumentNullException.ThrowIfNull(targetPoints);

            this.maxCartesianAlongRay = ArrayPool<uint>.Shared.Rent(401);

            Point2Int32 center = PointCollectionUtil.GetCenter(targetPoints);

            foreach (Point2Int32 point in targetPoints)
            {
                Point2Int32 offset = point.Subtract(center);

                uint cartesian = (uint)((offset.X * offset.X) + (offset.Y * offset.Y));

                uint ray = GetRadial(offset);
                if (cartesian > this.maxCartesianAlongRay[ray])
                {
                    this.maxCartesianAlongRay[ray] = cartesian;
                }
            }
            this.outward = outward;
        }

        public int Compare(Point2Int32 point1, Point2Int32 point2)
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref this.disposed) == 1, null);

            float point1Proportion = ProportionInward(point1);
            float point2Proportion = ProportionInward(point2);

            if (this.outward)
            {
                return point2Proportion.CompareTo(point1Proportion);
            }
            else
            {
                return point1Proportion.CompareTo(point2Proportion);
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref this.disposed, 1) == 0)
            {
                ArrayPool<uint>.Shared.Return(this.maxCartesianAlongRay);
            }
        }

        private readonly float ProportionInward(Point2Int32 point)
        {
            uint ray = GetRadial(point);

            return (float)((point.X * point.X) + (point.Y * point.Y)) / this.maxCartesianAlongRay[ray];
        }

        private static uint GetRadial(Point2Int32 point)
        {
            return (uint)((Math.Atan2(point.Y, point.X) * 200 / Math.PI) + 200);
        }
    }
}
