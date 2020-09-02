/*
*  This file is part of pdn-content-aware-fill, A Resynthesizer-based
*  content aware fill Effect plug-in for Paint.NET.
*
*  Copyright (C) 2018, 2020 Nicholas Hayes
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

using PaintDotNet;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace ContentAwareFill
{
    internal sealed class Resynthesizer : IDisposable
    {
        private const int ColorChannelCount = 3;
        private const byte MaskUnselected = 0;
        private const byte MaskFullySelected = 255;

        private readonly ResynthesizerParameters parameters;
        private readonly Random random;
        private readonly Action<int> progressCallback;
        private readonly ReadOnlyArray<ushort> diffTable;

#pragma warning disable IDE0032 // Use auto property
        private Surface target;
#pragma warning restore IDE0032 // Use auto property
        private Surface source;
        private MaskSurface targetMask;
        private MaskSurface sourceMask;

        private Neighbor[] neighbors;
        private int neighborCount;
        private RepetitionParameter[] repetitionParameters;
        private PointIndexedArray<int> tried;
        private PointIndexedArray<bool> hasValue;
        private PointIndexedArray<Point> sourceOf;
        private ReadOnlyList<Point> sortedOffsets;
        private ReadOnlyList<Point> targetPoints;
        private ReadOnlyList<Point> sourcePoints;
        private int targetTriesCount;
        private int totalTargets;
        private uint best;
        private Point bestPoint;
        private BettermentKind latestBettermentKind;

        /// <summary>
        /// Initializes a new instance of the <see cref="Resynthesizer"/> class.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="target">The target.</param>
        /// <param name="source">The source.</param>
        /// <param name="targetMask">The target mask.</param>
        /// <param name="sourceMask">The source mask.</param>
        /// <param name="sourceROI">The source region of interest.</param>
        /// <param name="croppedSourceSize">The size of the cropped source.</param>
        /// <param name="progressCallback">The progress callback.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="parameters"/> is null.
        /// or
        /// <paramref name="source"/> is null.
        /// or
        /// <paramref name="targetMask"/> is null.
        /// or
        /// <paramref name="sourceMask"/> is null.
        /// </exception>
        public Resynthesizer(ResynthesizerParameters parameters, Surface source, MaskSurface targetMask, MaskSurface sourceMask,
            Rectangle sourceROI, Size croppedSourceSize, Action<int> progressCallback)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (targetMask == null)
            {
                throw new ArgumentNullException(nameof(targetMask));
            }
            if (sourceMask == null)
            {
                throw new ArgumentNullException(nameof(sourceMask));
            }

            this.parameters = parameters;
            target = source.Clone();
            this.source = new Surface(croppedSourceSize.Width, croppedSourceSize.Height);
            this.source.CopySurface(source, sourceROI);
            this.targetMask = targetMask.Clone();
            this.sourceMask = new MaskSurface(croppedSourceSize.Width, croppedSourceSize.Height);
            this.sourceMask.CopySurface(sourceMask, sourceROI);

            random = new Random(1198472);
            diffTable = MakeDiffTable(parameters);
            neighbors = new Neighbor[parameters.Neighbors];
            repetitionParameters = new RepetitionParameter[ResynthesizerConstants.MaxPasses];
            tried = new PointIndexedArray<int>(target.Width, target.Height, -1);
            hasValue = new PointIndexedArray<bool>(target.Width, target.Height, false);
            sourceOf = new PointIndexedArray<Point>(target.Width, target.Height, new Point(-1, -1));
            this.progressCallback = progressCallback;
        }

        private enum BettermentKind
        {
            None,
            NeighborSource,
            RandomSource,
            PerfectMatch
        }

        public Surface Target
        {
            get
            {
                return target;
            }
        }

        public void Dispose()
        {
            if (target != null)
            {
                target.Dispose();
                target = null;
            }
            if (source != null)
            {
                source.Dispose();
                source = null;
            }
            if (targetMask != null)
            {
                targetMask.Dispose();
                targetMask = null;
            }
            if (sourceMask != null)
            {
                sourceMask.Dispose();
                sourceMask = null;
            }
        }

        /// <summary>
        /// Performs the content the aware fill.
        /// </summary>
        /// <param name="abortCallback">The abort callback.</param>
        /// <returns><c>true</c> on success; otherwise <c>false</c> if the user canceled rendering.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="abortCallback"/> is null.</exception>
        /// <exception cref="ResynthizerException">
        /// The source selection is empty.
        /// or
        /// The destination layer is empty.
        /// </exception>
        public bool ContentAwareFill(Func<bool> abortCallback)
        {
            if (abortCallback == null)
            {
                throw new ArgumentNullException(nameof(abortCallback));
            }

            PrepareTargetPoints(parameters.MatchContext != MatchContextType.None);
            // The constructor handles the setup performed by prepare_target_sources.
            PrepareSourcePoints();

            if (sourcePoints.Count == 0)
            {
                throw new ResynthizerException(Properties.Resources.SourcePointsEmpty);
            }
            if (targetPoints.Count == 0)
            {
                throw new ResynthizerException(Properties.Resources.TargetPointsEmpty);
            }

            PrepareSortedOffsets();
            // The constructor handles the setup performed by prepare_tried.
            PrepareRepetitionParameters();

            for (int i = 0; i < ResynthesizerConstants.MaxPasses; i++)
            {
                int? betters = Synthesize(i, abortCallback);

                if (!betters.HasValue)
                {
                    return false;
                }

                if (((float)betters.Value / (float)targetPoints.Count) < ResynthesizerConstants.TerminateFraction)
                {
                    break;
                }
            }

            return true;
        }

        private bool ClippedOrMaskedSource(Point point)
        {
            return point.X < 0 ||
                   point.Y < 0 ||
                   point.X >= source.Width ||
                   point.Y >= source.Height ||
                   sourceMask.GetPointUnchecked(point.X, point.Y) != MaskFullySelected;
        }

        private static ReadOnlyArray<ushort> MakeDiffTable(ResynthesizerParameters parameters)
        {
            ushort[] diffTable = new ushort[512];

            double valueDivisor = NegLogCauchy(1.0 / parameters.SensitivityToOutliers);

            for (int i = -256; i < 256; i++)
            {
                double value = NegLogCauchy(i / 256.0 / parameters.SensitivityToOutliers) / valueDivisor * ResynthesizerConstants.MaxWeight;

                diffTable[256 + i] = (ushort)value;
                // The original code populated a map diff table array that is used to when mapping between images.
                // This is not required for the content aware fill functionality.
            }

            return new ReadOnlyArray<ushort>(diffTable);
        }

        private static double NegLogCauchy(double d)
        {
            return Math.Log((d * d) + 1.0);
        }

        private void PrepareNeighbors(Point position)
        {
            neighborCount = 0;

            for (int i = 0; i < sortedOffsets.Count; i++)
            {
                Point offset = sortedOffsets[i];
                Point neighborPoint = position.Add(offset);

                if (WrapOrClip(target, ref neighborPoint) && hasValue[neighborPoint])
                {
                    neighbors[neighborCount] = new Neighbor(target.GetPoint(neighborPoint.X, neighborPoint.Y), offset, sourceOf[neighborPoint]);
                    neighborCount++;

                    if (neighborCount >= parameters.Neighbors)
                    {
                        break;
                    }
                }
            }
        }

        private void PrepareRepetitionParameters()
        {
            repetitionParameters[0] = new RepetitionParameter(0, targetPoints.Count);

            totalTargets = targetPoints.Count;
            int n = totalTargets;

            for (int i = 1; i < ResynthesizerConstants.MaxPasses; i++)
            {
                repetitionParameters[i] = new RepetitionParameter(0, n);
                totalTargets += n;
                n = n * 3 / 4;
            }
        }

        private void PrepareSortedOffsets()
        {
            int width = Math.Min(target.Width, source.Width);
            int height = Math.Min(target.Height, source.Height);

            int length = ((2 * width) - 1) * ((2 * height) - 1);

            List<Point> offsets = new List<Point>(length);

            for (int y = -height + 1; y < height; y++)
            {
                for (int x = -width + 1; x < width; x++)
                {
                    offsets.Add(new Point(x, y));
                }
            }

            offsets.Sort(PointComparer.LessCartesian);

            sortedOffsets = new ReadOnlyList<Point>(offsets);
        }

        private unsafe void PrepareTargetPoints(bool useContext)
        {
            int targetPointsSize = 0;

            for (int y = 0; y < target.Height; y++)
            {
                byte* mask = targetMask.GetRowAddressUnchecked(y);

                for (int x = 0; x < target.Width; x++)
                {
                    if (*mask != MaskUnselected)
                    {
                        targetPointsSize++;
                    }

                    mask++;
                }
            }

            List<Point> points = new List<Point>(targetPointsSize);

            if (targetPointsSize > 0)
            {
                for (int y = 0; y < target.Height; y++)
                {
                    ColorBgra* src = target.GetRowAddressUnchecked(y);
                    byte* mask = targetMask.GetRowAddressUnchecked(y);

                    for (int x = 0; x < target.Width; x++)
                    {
                        bool isSelectedTarget = *mask != MaskUnselected;

                        hasValue.SetValue(x, y, useContext && !isSelectedTarget && src->A > 0);

                        if (isSelectedTarget)
                        {
                            points.Add(new Point(x, y));
                        }

                        src++;
                        mask++;
                    }
                }

                points = TargetPointSorter.Sort(points, random, parameters.MatchContext);
            }

            targetPoints = new ReadOnlyList<Point>(points);
        }

        private unsafe void PrepareSourcePoints()
        {
            List<Point> points = new List<Point>(source.Width * source.Height);

            for (int y = 0; y < source.Height; y++)
            {
                ColorBgra* src = source.GetRowAddressUnchecked(y);
                byte* mask = sourceMask.GetRowAddressUnchecked(y);

                for (int x = 0; x < source.Width; x++)
                {
                    if (*mask == MaskFullySelected && src->A > 0)
                    {
                        points.Add(new Point(x, y));
                    }

                    src++;
                    mask++;
                }
            }

            sourcePoints = new ReadOnlyList<Point>(points);
        }

        private Point RandomSourcePoint()
        {
            int index = random.Next(0, sourcePoints.Count);

            return sourcePoints[index];
        }

        private bool TryPoint(Point point, BettermentKind bettermentKind)
        {
            uint sum = 0;

            for (int i = 0; i < neighborCount; i++)
            {
                Point offset = point.Add(neighbors[i].offset);

                if (ClippedOrMaskedSource(offset))
                {
                    // The original code used the map_diff_table in the following calculation.
                    // sum += MaxWeight * ColorChannelCount + map_diff_table[0] * map_match_bpp
                    //
                    // As the map_match_bpp variable would always be zero in this plug-in the
                    // above calculation can be simplified to remove the map_diff_table.
                    sum += ResynthesizerConstants.MaxWeight * ColorChannelCount;
                }
                else
                {
                    ColorBgra sourcePixel = source.GetPoint(offset.X, offset.Y);
                    ColorBgra targetPixel = neighbors[i].pixel;

                    if (i > 0)
                    {
                        sum += diffTable[256 + targetPixel.B - sourcePixel.B];
                        sum += diffTable[256 + targetPixel.G - sourcePixel.G];
                        sum += diffTable[256 + targetPixel.R - sourcePixel.R];
                    }
                    // The original code would add the map_diff_table values to the sum when
                    // the map image is used.
                    // That code is not used for the content aware fill functionality.
                }

                if (sum >= best)
                {
                    return false;
                }
            }

            best = sum;
            latestBettermentKind = bettermentKind;
            bestPoint = point;

            return sum <= 0;
        }

        private bool WrapOrClip(Surface image, ref Point point)
        {
            while (point.X < 0)
            {
                if (parameters.TileHorizontal)
                {
                    point.X += image.Width;
                }
                else
                {
                    return false;
                }
            }

            while (point.X >= image.Width)
            {
                if (parameters.TileHorizontal)
                {
                    point.X -= image.Width;
                }
                else
                {
                    return false;
                }
            }

            while (point.Y < 0)
            {
                if (parameters.TileVertical)
                {
                    point.Y += image.Height;
                }
                else
                {
                    return false;
                }
            }

            while (point.Y >= image.Height)
            {
                if (parameters.TileVertical)
                {
                    point.Y -= image.Height;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// The core function that performs the image synthesis.
        /// </summary>
        /// <param name="pass">The pass.</param>
        /// <param name="abortCallback">The abort callback.</param>
        /// <returns>The match count of the image synthesis; or <c>null</c> if the user canceled the operation.</returns>
        private int? Synthesize(int pass, Func<bool> abortCallback)
        {
            int length = repetitionParameters[pass].end;
            int repeatCountBetters = 0;
            bool perfectMatch;

            for (int targetIndex = 0; targetIndex < length; targetIndex++)
            {
                // Report progress in increments of 4096.
                if ((targetIndex & 4095) == 0)
                {
                    if (abortCallback())
                    {
                        return null;
                    }
                    if (progressCallback != null)
                    {
                        targetTriesCount += 4096;

                        double value = ((double)targetTriesCount / (double)totalTargets) * 100.0;

                        progressCallback((int)value.Clamp(0.0, 100.0));
                    }
                }

                Point position = targetPoints[targetIndex];

                hasValue[position] = true;

                PrepareNeighbors(position);

                best = uint.MaxValue;
                latestBettermentKind = BettermentKind.None;
                perfectMatch = false;

                for (int neighborIndex = 0; neighborIndex < neighborCount; neighborIndex++)
                {
                    Neighbor neighbor = neighbors[neighborIndex];
                    if (neighbor.sourceOf.X != -1)
                    {
                        Point sourcePoint = neighbor.sourceOf.Subtract(neighbor.offset);

                        if (ClippedOrMaskedSource(sourcePoint))
                        {
                            continue;
                        }
                        if (tried[sourcePoint] == targetIndex)
                        {
                            continue;
                        }

                        perfectMatch = TryPoint(sourcePoint, BettermentKind.NeighborSource);
                        if (perfectMatch)
                        {
                            break;
                        }

                        tried[sourcePoint] = targetIndex;
                    }
                }

                if (!perfectMatch)
                {
                    for (uint i = 0; i < parameters.Trys; i++)
                    {
                        perfectMatch = TryPoint(RandomSourcePoint(), BettermentKind.RandomSource);
                        if (perfectMatch)
                        {
                            break;
                        }
                    }
                }

                if (latestBettermentKind != BettermentKind.None)
                {
                    if (sourceOf[position] != bestPoint)
                    {
                        repeatCountBetters++;

                        target[position] = source[bestPoint];
                        sourceOf[position] = bestPoint;
                    }
                }
            }

            return repeatCountBetters;
        }

        private struct Neighbor
        {
            public readonly ColorBgra pixel;
            public readonly Point offset;
            public readonly Point sourceOf;

            public Neighbor(ColorBgra pixel, Point offset, Point sourceOf)
            {
                this.pixel = pixel;
                this.offset = offset;
                this.sourceOf = sourceOf;
            }
        }

        private struct RepetitionParameter
        {
            public readonly int start;
            public readonly int end;

            public RepetitionParameter(int start, int end)
            {
                this.start = start;
                this.end = end;
            }
        }
    }
}
