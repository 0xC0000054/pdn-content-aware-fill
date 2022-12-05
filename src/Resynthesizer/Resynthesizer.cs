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
            this.target = source.Clone();
            this.source = new Surface(croppedSourceSize.Width, croppedSourceSize.Height);
            this.source.CopySurface(source, sourceROI);
            this.targetMask = targetMask.Clone();
            this.sourceMask = new MaskSurface(croppedSourceSize.Width, croppedSourceSize.Height);
            this.sourceMask.CopySurface(sourceMask, sourceROI);

            this.random = new Random(1198472);
            this.diffTable = MakeDiffTable(parameters);
            this.neighbors = new Neighbor[parameters.Neighbors];
            this.repetitionParameters = new RepetitionParameter[ResynthesizerConstants.MaxPasses];
            this.tried = new PointIndexedArray<int>(this.target.Width, this.target.Height, -1);
            this.hasValue = new PointIndexedArray<bool>(this.target.Width, this.target.Height, false);
            this.sourceOf = new PointIndexedArray<Point>(this.target.Width, this.target.Height, new Point(-1, -1));
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
                return this.target;
            }
        }

        public void Dispose()
        {
            if (this.target != null)
            {
                this.target.Dispose();
                this.target = null;
            }
            if (this.source != null)
            {
                this.source.Dispose();
                this.source = null;
            }
            if (this.targetMask != null)
            {
                this.targetMask.Dispose();
                this.targetMask = null;
            }
            if (this.sourceMask != null)
            {
                this.sourceMask.Dispose();
                this.sourceMask = null;
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

            PrepareTargetPoints(this.parameters.MatchContext != MatchContextType.None);
            // The constructor handles the setup performed by prepare_target_sources.
            PrepareSourcePoints();

            if (this.sourcePoints.Count == 0)
            {
                throw new ResynthizerException(Properties.Resources.SourcePointsEmpty);
            }
            if (this.targetPoints.Count == 0)
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

                if (((float)betters.Value / this.targetPoints.Count) < ResynthesizerConstants.TerminateFraction)
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
                   point.X >= this.source.Width ||
                   point.Y >= this.source.Height ||
                   this.sourceMask.GetPointUnchecked(point.X, point.Y) != MaskFullySelected;
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
            this.neighborCount = 0;

            for (int i = 0; i < this.sortedOffsets.Count; i++)
            {
                Point offset = this.sortedOffsets[i];
                Point neighborPoint = position.Add(offset);

                if (WrapOrClip(this.target, ref neighborPoint) && this.hasValue[neighborPoint])
                {
                    this.neighbors[this.neighborCount] = new Neighbor(this.target.GetPoint(neighborPoint.X, neighborPoint.Y), offset, this.sourceOf[neighborPoint]);
                    this.neighborCount++;

                    if (this.neighborCount >= this.parameters.Neighbors)
                    {
                        break;
                    }
                }
            }
        }

        private void PrepareRepetitionParameters()
        {
            this.repetitionParameters[0] = new RepetitionParameter(0, this.targetPoints.Count);

            this.totalTargets = this.targetPoints.Count;
            int n = this.totalTargets;

            for (int i = 1; i < ResynthesizerConstants.MaxPasses; i++)
            {
                this.repetitionParameters[i] = new RepetitionParameter(0, n);
                this.totalTargets += n;
                n = n * 3 / 4;
            }
        }

        private void PrepareSortedOffsets()
        {
            int width = Math.Min(this.target.Width, this.source.Width);
            int height = Math.Min(this.target.Height, this.source.Height);

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

            this.sortedOffsets = new ReadOnlyList<Point>(offsets);
        }

        private unsafe void PrepareTargetPoints(bool useContext)
        {
            int targetPointsSize = 0;

            for (int y = 0; y < this.target.Height; y++)
            {
                byte* mask = this.targetMask.GetRowAddressUnchecked(y);

                for (int x = 0; x < this.target.Width; x++)
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
                for (int y = 0; y < this.target.Height; y++)
                {
                    ColorBgra* src = this.target.GetRowAddressUnchecked(y);
                    byte* mask = this.targetMask.GetRowAddressUnchecked(y);

                    for (int x = 0; x < this.target.Width; x++)
                    {
                        bool isSelectedTarget = *mask != MaskUnselected;

                        this.hasValue.SetValue(x, y, useContext && !isSelectedTarget && src->A > 0);

                        if (isSelectedTarget)
                        {
                            points.Add(new Point(x, y));
                        }

                        src++;
                        mask++;
                    }
                }

                points = TargetPointSorter.Sort(points, this.random, this.parameters.MatchContext);
            }

            this.targetPoints = new ReadOnlyList<Point>(points);
        }

        private unsafe void PrepareSourcePoints()
        {
            List<Point> points = new List<Point>(this.source.Width * this.source.Height);

            for (int y = 0; y < this.source.Height; y++)
            {
                ColorBgra* src = this.source.GetRowAddressUnchecked(y);
                byte* mask = this.sourceMask.GetRowAddressUnchecked(y);

                for (int x = 0; x < this.source.Width; x++)
                {
                    if (*mask == MaskFullySelected && src->A > 0)
                    {
                        points.Add(new Point(x, y));
                    }

                    src++;
                    mask++;
                }
            }

            this.sourcePoints = new ReadOnlyList<Point>(points);
        }

        private Point RandomSourcePoint()
        {
            int index = this.random.Next(0, this.sourcePoints.Count);

            return this.sourcePoints[index];
        }

        private bool TryPoint(Point point, BettermentKind bettermentKind)
        {
            uint sum = 0;

            for (int i = 0; i < this.neighborCount; i++)
            {
                Point offset = point.Add(this.neighbors[i].offset);

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
                    ColorBgra sourcePixel = this.source.GetPoint(offset.X, offset.Y);
                    ColorBgra targetPixel = this.neighbors[i].pixel;

                    if (i > 0)
                    {
                        sum += this.diffTable[256 + targetPixel.B - sourcePixel.B];
                        sum += this.diffTable[256 + targetPixel.G - sourcePixel.G];
                        sum += this.diffTable[256 + targetPixel.R - sourcePixel.R];
                    }
                    // The original code would add the map_diff_table values to the sum when
                    // the map image is used.
                    // That code is not used for the content aware fill functionality.
                }

                if (sum >= this.best)
                {
                    return false;
                }
            }

            this.best = sum;
            this.latestBettermentKind = bettermentKind;
            this.bestPoint = point;

            return sum <= 0;
        }

        private bool WrapOrClip(Surface image, ref Point point)
        {
            while (point.X < 0)
            {
                if (this.parameters.TileHorizontal)
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
                if (this.parameters.TileHorizontal)
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
                if (this.parameters.TileVertical)
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
                if (this.parameters.TileVertical)
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
            int length = this.repetitionParameters[pass].end;
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
                    if (this.progressCallback != null)
                    {
                        this.targetTriesCount += 4096;

                        double value = ((double)this.targetTriesCount / (double)this.totalTargets) * 100.0;

                        this.progressCallback((int)value.Clamp(0.0, 100.0));
                    }
                }

                Point position = this.targetPoints[targetIndex];

                this.hasValue[position] = true;

                PrepareNeighbors(position);

                this.best = uint.MaxValue;
                this.latestBettermentKind = BettermentKind.None;
                perfectMatch = false;

                for (int neighborIndex = 0; neighborIndex < this.neighborCount; neighborIndex++)
                {
                    Neighbor neighbor = this.neighbors[neighborIndex];
                    if (neighbor.sourceOf.X != -1)
                    {
                        Point sourcePoint = neighbor.sourceOf.Subtract(neighbor.offset);

                        if (ClippedOrMaskedSource(sourcePoint))
                        {
                            continue;
                        }
                        if (this.tried[sourcePoint] == targetIndex)
                        {
                            continue;
                        }

                        perfectMatch = TryPoint(sourcePoint, BettermentKind.NeighborSource);
                        if (perfectMatch)
                        {
                            break;
                        }

                        this.tried[sourcePoint] = targetIndex;
                    }
                }

                if (!perfectMatch)
                {
                    for (uint i = 0; i < this.parameters.Trys; i++)
                    {
                        perfectMatch = TryPoint(RandomSourcePoint(), BettermentKind.RandomSource);
                        if (perfectMatch)
                        {
                            break;
                        }
                    }
                }

                if (this.latestBettermentKind != BettermentKind.None)
                {
                    if (this.sourceOf[position] != this.bestPoint)
                    {
                        repeatCountBetters++;

                        this.target[position] = this.source[this.bestPoint];
                        this.sourceOf[position] = this.bestPoint;
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
