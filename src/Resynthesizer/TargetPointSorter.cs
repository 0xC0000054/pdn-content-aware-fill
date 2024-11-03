/*
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

using Collections.Pooled;
using PaintDotNet.Rendering;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;

namespace ContentAwareFill
{
    internal static class TargetPointSorter
    {
        internal static void Sort(PooledList<Point2Int32> points, Random random, MatchContextType matchContextType)
        {
            switch (matchContextType)
            {
                case MatchContextType.None:
                case MatchContextType.Random:
                    OrderTargetPointsRandom(points, random);
                    break;
                case MatchContextType.InwardConcentric:
                    using (DirectionalPointComparer comparer = new(points, outward: false))
                    {
                        OrderTargetPointsRandomDirectional(points, random, comparer);
                    }
                    break;
                case MatchContextType.InwardHorizontal:
                    OrderTargetPointsRandomDirectional(points, random, new HorizontalPointComparer(outward: false));
                    break;
                case MatchContextType.InwardVertical:
                    OrderTargetPointsRandomDirectional(points, random, new VerticalPointComparer(outward: false));
                    break;
                case MatchContextType.OutwardConcentric:
                    using (DirectionalPointComparer comparer = new(points, outward: true))
                    {
                        OrderTargetPointsRandomDirectional(points, random, comparer);
                    }
                    break;
                case MatchContextType.OutwardHorizontal:
                    OrderTargetPointsRandomDirectional(points, random, new HorizontalPointComparer(outward: true));
                    break;
                case MatchContextType.OutwardVertical:
                    OrderTargetPointsRandomDirectional(points, random, new VerticalPointComparer(outward: true));
                    break;
                default:
                    throw new InvalidEnumArgumentException(nameof(matchContextType), (int)matchContextType, typeof(MatchContextType));
            }
        }

        private static void OrderTargetPointsRandom(PooledList<Point2Int32> points, Random random)
        {
            int count = points.Count;

            for (int i = 0; i < count; i++)
            {
                int j = random.Next(0, count);

                (points[j], points[i]) = (points[i], points[j]);
            }
        }

        private static void TargetPointsToOffsets(PooledList<Point2Int32> points, Point2Int32 center)
        {
            for (int i = 0; i < points.Count; i++)
            {
                points[i] = points[i].Subtract(center);
            }
        }

        private static void TargetPointsFromOffsets(PooledList<Point2Int32> points, Point2Int32 center)
        {
            for (int i = 0; i < points.Count; i++)
            {
                points[i] = points[i].Add(center);
            }
        }

        private static void RandomizeBandsTargetPoints(PooledList<Point2Int32> points, Random random)
        {
            int last = points.Count - 1;
            int halfBand = (int)(points.Count * ResynthesizerConstants.BandFraction);

            for (int i = 0; i <= last; i++)
            {
                int bandStart = Math.Max(i - halfBand, 0);
                int bandEnd = Math.Min(i + halfBand, last);
                int bandSize = bandEnd - bandStart;

                int j = bandStart + random.Next(0, bandSize);

                (points[j], points[i]) = (points[i], points[j]);
            }
        }

        private static void OrderTargetPointsRandomDirectional<TComparer>(PooledList<Point2Int32> points,
                                                                          Random random,
                                                                          TComparer pointComparer) where TComparer : struct, IComparer<Point2Int32>
        {
            Point2Int32 center = PointCollectionUtil.GetCenter(points);

            TargetPointsToOffsets(points, center);

            points.Span.Sort(pointComparer.Compare);

            TargetPointsFromOffsets(points, center);

            RandomizeBandsTargetPoints(points, random);
        }
    }
}
