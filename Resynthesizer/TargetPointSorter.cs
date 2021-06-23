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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

namespace ContentAwareFill
{
    internal static class TargetPointSorter
    {
        internal static List<Point> Sort(List<Point> points, Random random, MatchContextType matchContextType)
        {
            switch (matchContextType)
            {
                case MatchContextType.None:
                case MatchContextType.Random:
                    points = OrderTargetPointsRandom(points, random);
                    break;
                case MatchContextType.InwardConcentric:
                    points = OrderTargetPointsRandomDirectional(points, random, PointComparer.CreateInwardConcentric(points));
                    break;
                case MatchContextType.InwardHorizontal:
                    points = OrderTargetPointsRandomDirectional(points, random, PointComparer.InwardHorizontal);
                    break;
                case MatchContextType.InwardVertical:
                    points = OrderTargetPointsRandomDirectional(points, random, PointComparer.InwardVertical);
                    break;
                case MatchContextType.OutwardConcentric:
                    points = OrderTargetPointsRandomDirectional(points, random, PointComparer.CreateOutwardConcentric(points));
                    break;
                case MatchContextType.OutwardHorizontal:
                    points = OrderTargetPointsRandomDirectional(points, random, PointComparer.OutwardHorizontal);
                    break;
                case MatchContextType.OutwardVertical:
                    points = OrderTargetPointsRandomDirectional(points, random, PointComparer.OutwardVertical);
                    break;
                default:
                    throw new InvalidEnumArgumentException(nameof(matchContextType), (int)matchContextType, typeof(MatchContextType));
            }

            return points;
        }

        private static List<Point> OrderTargetPointsRandom(List<Point> points, Random random)
        {
            int count = points.Count;

            for (int i = 0; i < count; i++)
            {
                int j = random.Next(0, count);

                Point temp = points[i];
                points[i] = points[j];
                points[j] = temp;
            }

            return points;
        }

        private static List<Point> TargetPointsToOffsets(List<Point> points, Point center)
        {
            for (int i = 0; i < points.Count; i++)
            {
                points[i] = points[i].Subtract(center);
            }

            return points;
        }

        private static List<Point> TargetPointsFromOffsets(List<Point> points, Point center)
        {
            for (int i = 0; i < points.Count; i++)
            {
                points[i] = points[i].Add(center);
            }

            return points;
        }

        private static List<Point> RandomizeBandsTargetPoints(List<Point> points, Random random)
        {
            int last = points.Count - 1;
            int halfBand = (int)(points.Count * ResynthesizerConstants.BandFraction);

            for (int i = 0; i <= last; i++)
            {
                int bandStart = Math.Max(i - halfBand, 0);
                int bandEnd = Math.Min(i + halfBand, last);
                int bandSize = bandEnd - bandStart;

                int j = bandStart + random.Next(0, bandSize);

                Point temp = points[i];
                points[i] = points[j];
                points[j] = temp;
            }

            return points;
        }

        private static List<Point> OrderTargetPointsRandomDirectional(List<Point> points, Random random, IComparer<Point> pointComparer)
        {
            Point center = PointCollectionUtil.GetCenter(points);

            points = TargetPointsToOffsets(points, center);

            points.Sort(pointComparer);

            points = TargetPointsFromOffsets(points, center);

            return RandomizeBandsTargetPoints(points, random);
        }
    }
}
