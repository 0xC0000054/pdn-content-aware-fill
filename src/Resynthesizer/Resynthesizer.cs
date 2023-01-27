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
using PaintDotNet.Effects;
using PaintDotNet.Imaging;
using PaintDotNet.Rendering;
using System;
using System.Collections.Immutable;

namespace ContentAwareFill
{
    internal sealed class Resynthesizer : IDisposable
    {
        private const int ColorChannelCount = 3;

        private readonly ResynthesizerParameters parameters;
        private readonly Random random;
        private readonly Action<int> progressCallback;
        private readonly ImmutableArray<ushort> diffTable;

#pragma warning disable IDE0032 // Use auto property
        private IBitmap<ColorBgra32> target;
#pragma warning restore IDE0032 // Use auto property
        private IBitmap<ColorBgra32> source;
        private IBitmap<ColorAlpha8> targetMask;
        private IBitmap<ColorAlpha8> sourceMask;

        private readonly Neighbor[] neighbors;
        private int neighborCount;
        private readonly RepetitionParameter[] repetitionParameters;
        private readonly PointIndexedArray<int> tried;
        private readonly PointIndexedArray<bool> hasValue;
        private readonly PointIndexedArray<Point2Int32> sourceOf;
        private ImmutableArray<Point2Int32> sortedOffsets;
        private ImmutableArray<Point2Int32> targetPoints;
        private ImmutableArray<Point2Int32> sourcePoints;
        private int targetTriesCount;
        private int totalTargets;
        private uint best;
        private Point2Int32 bestPoint;
        private BettermentKind latestBettermentKind;

        /// <summary>
        /// Initializes a new instance of the <see cref="Resynthesizer"/> class.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="source">The source.</param>
        /// <param name="sourceMask">The source mask.</param>
        /// <param name="sourceRoi">The source region of interest.</param>
        /// <param name="croppedSourceSize">The size of the cropped source.</param>
        /// <param name="targetMask">The target mask.</param>
        /// <param name="progressCallback">The progress callback.</param>
        /// <param name="imagingFactory">The imaging factory.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="parameters"/> is null.
        /// or
        /// <paramref name="source"/> is null.
        /// or
        /// <paramref name="sourceMask"/> is null.
        /// or
        /// <paramref name="targetMask"/> is null.
        /// or
        /// <paramref name="imagingFactory"/> is null.
        /// </exception>
        public Resynthesizer(ResynthesizerParameters parameters,
                             IEffectInputBitmap<ColorBgra32> source,
                             IBitmap<ColorAlpha8> sourceMask,
                             RectInt32 sourceRoi,
                             SizeInt32 croppedSourceSize,
                             IBitmap<ColorAlpha8> targetMask,
                             Action<int> progressCallback,
                             IImagingFactory imagingFactory)
        {
            ArgumentNullException.ThrowIfNull(parameters);
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(sourceMask);
            ArgumentNullException.ThrowIfNull(targetMask);
            ArgumentNullException.ThrowIfNull(imagingFactory);

            if (targetMask.Size != source.Size)
            {
                throw new ArgumentException("The target mask size does not match the source image.");
            }

            this.parameters = parameters;
            this.target = BitmapUtil.CreateFromBitmapSource(imagingFactory, source);
            this.source = BitmapUtil.CreateFromBitmapSource(imagingFactory, source, croppedSourceSize, sourceRoi);
            this.sourceMask = BitmapUtil.CreateFromBitmapSource(imagingFactory, sourceMask, croppedSourceSize, sourceRoi, clear: true);
            this.targetMask = targetMask.CreateRefT();

            this.random = new Random(1198472);
            this.diffTable = MakeDiffTable(parameters);
            this.neighbors = new Neighbor[parameters.Neighbors];
            this.repetitionParameters = new RepetitionParameter[ResynthesizerConstants.MaxPasses];
            this.tried = new PointIndexedArray<int>(this.target.Size, -1);
            this.hasValue = new PointIndexedArray<bool>(this.target.Size, false);
            this.sourceOf = new PointIndexedArray<Point2Int32>(this.target.Size, new Point2Int32(-1, -1));
            this.progressCallback = progressCallback;
        }

        private enum BettermentKind
        {
            None,
            NeighborSource,
            RandomSource,
            PerfectMatch
        }

        public IBitmap<ColorBgra32> Target
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

            if (this.sourcePoints.Length == 0)
            {
                throw new ResynthizerException(Properties.Resources.SourcePointsEmpty);
            }

            if (this.targetPoints.Length == 0)
            {
                throw new ResynthizerException(Properties.Resources.TargetPointsEmpty);
            }

            PrepareSortedOffsets();
            // The constructor handles the setup performed by prepare_tried.
            PrepareRepetitionParameters();

            using (IBitmapLock<ColorBgra32> sourceLock = this.source.Lock(BitmapLockOptions.Read))
            using (IBitmapLock<ColorAlpha8> sourceMaskLock = this.sourceMask.Lock(BitmapLockOptions.Read))
            using (IBitmapLock<ColorBgra32> targetLock = this.target.Lock(BitmapLockOptions.Write))
            {
                RegionPtr<ColorBgra32> sourceRegion = sourceLock.AsRegionPtr();
                RegionPtr<ColorAlpha8> sourceMaskRegion = sourceMaskLock.AsRegionPtr();
                RegionPtr<ColorBgra32> targetRegion = targetLock.AsRegionPtr();

                for (int i = 0; i < ResynthesizerConstants.MaxPasses; i++)
                {
                    int? betters = Synthesize(i, abortCallback, sourceRegion, sourceMaskRegion, targetRegion);

                    if (!betters.HasValue)
                    {
                        return false;
                    }

                    if (((float)betters.Value / this.targetPoints.Length) < ResynthesizerConstants.TerminateFraction)
                    {
                        break;
                    }
                }
            }

            return true;
        }

        private static bool ClippedOrMaskedSource(Point2Int32 point, RegionPtr<ColorAlpha8> sourceMaskRegion)
        {
            return point.X < 0 ||
                   point.Y < 0 ||
                   point.X >= sourceMaskRegion.Width ||
                   point.Y >= sourceMaskRegion.Height ||
                   sourceMaskRegion[point.X, point.Y] != ColorAlpha8.Opaque;
        }

        private static ImmutableArray<ushort> MakeDiffTable(ResynthesizerParameters parameters)
        {
            ImmutableArray<ushort>.Builder diffTable = ImmutableArray.CreateBuilder<ushort>(512);

            double valueDivisor = NegLogCauchy(1.0 / parameters.SensitivityToOutliers);

            for (int i = -256; i < 256; i++)
            {
                double value = NegLogCauchy(i / 256.0 / parameters.SensitivityToOutliers) / valueDivisor * ResynthesizerConstants.MaxWeight;

                diffTable.Insert(256 + i, (ushort)value);
                // The original code populated a map diff table array that is used to when mapping between images.
                // This is not required for the content aware fill functionality.
            }

            return diffTable.MoveToImmutable();
        }

        private static double NegLogCauchy(double d)
        {
            return Math.Log((d * d) + 1.0);
        }

        private void PrepareNeighbors(Point2Int32 position)
        {
            this.neighborCount = 0;

            using (IBitmapLock<ColorBgra32> bitmapLock = this.target.Lock(BitmapLockOptions.Read))
            {
                RegionPtr<ColorBgra32> region = bitmapLock.AsRegionPtr();
                SizeInt32 targetSize = region.Size;

                for (int i = 0; i < this.sortedOffsets.Length; i++)
                {
                    Point2Int32 offset = this.sortedOffsets[i];
                    Point2Int32 neighborPoint = position.Add(offset);

                    if (WrapOrClip(targetSize, ref neighborPoint) && this.hasValue[neighborPoint])
                    {
                        this.neighbors[this.neighborCount] = new Neighbor(region[neighborPoint.X, neighborPoint.Y],
                                                                          offset,
                                                                          this.sourceOf[neighborPoint]);
                        this.neighborCount++;

                        if (this.neighborCount >= this.parameters.Neighbors)
                        {
                            break;
                        }
                    }
                }
            }
        }

        private void PrepareRepetitionParameters()
        {
            this.repetitionParameters[0] = new RepetitionParameter(0, this.targetPoints.Length);

            this.totalTargets = this.targetPoints.Length;
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
            SizeInt32 sourceSize = this.source.Size;

            int width = sourceSize.Width;
            int height = sourceSize.Height;

            int length = ((2 * width) - 1) * ((2 * height) - 1);

            ImmutableArray<Point2Int32>.Builder offsets = ImmutableArray.CreateBuilder<Point2Int32>(length);

            for (int y = -height + 1; y < height; y++)
            {
                for (int x = -width + 1; x < width; x++)
                {
                    offsets.Add(new Point2Int32(x, y));
                }
            }

            offsets.Sort(PointComparer.LessCartesian);

            this.sortedOffsets = offsets.MoveToImmutable();
        }

        private unsafe void PrepareTargetPoints(bool useContext)
        {
            using (IBitmapLock<ColorAlpha8> targetMaskLock = this.targetMask.Lock(BitmapLockOptions.Read))
            {
                RegionPtr<ColorAlpha8> targetMaskRegion = targetMaskLock.AsRegionPtr();

                int targetPointsSize = 0;

                SizeInt32 targetSize = this.target.Size;

                foreach (RegionRowPtr<ColorAlpha8> row in targetMaskRegion.Rows)
                {
                    foreach (ColorAlpha8 pixel in row)
                    {
                        if (pixel != ColorAlpha8.Transparent)
                        {
                            targetPointsSize++;
                        }
                    }
                }

                ImmutableArray<Point2Int32>.Builder points = ImmutableArray.CreateBuilder<Point2Int32>(targetPointsSize);

                if (targetPointsSize > 0)
                {
                    using (IBitmapLock<ColorBgra32> targetLock = this.target.Lock(BitmapLockOptions.Read))
                    {
                        RegionPtr<ColorBgra32> targetRegion = targetLock.AsRegionPtr();
                        RegionRowPtrCollection<ColorBgra32> targetRows = targetRegion.Rows;
                        RegionRowPtrCollection<ColorAlpha8> targetMaskRows = targetMaskRegion.Rows;

                        for (int y = 0; y < targetSize.Height; y++)
                        {
                            ColorBgra32* src = targetRows[y].Ptr;
                            ColorAlpha8* mask = targetMaskRows[y].Ptr;

                            for (int x = 0; x < targetSize.Width; x++)
                            {
                                bool isSelectedTarget = *mask != ColorAlpha8.Transparent;

                                this.hasValue.SetValue(x, y, useContext && !isSelectedTarget && src->A > 0);

                                if (isSelectedTarget)
                                {
                                    points.Add(new Point2Int32(x, y));
                                }

                                src++;
                                mask++;
                            }
                        }
                    }

                    TargetPointSorter.Sort(points, this.random, this.parameters.MatchContext);
                }

                this.targetPoints = points.MoveToImmutable();
            }
        }

        private unsafe void PrepareSourcePoints()
        {
            using (IBitmapLock<ColorBgra32> sourceLock = this.source.Lock(BitmapLockOptions.Read))
            using (IBitmapLock<ColorAlpha8> sourceMaskLock = this.sourceMask.Lock(BitmapLockOptions.Read))
            {
                RegionPtr<ColorBgra32> sourceRegion = sourceLock.AsRegionPtr();
                RegionPtr<ColorAlpha8> sourceMaskRegion = sourceMaskLock.AsRegionPtr();

                RegionRowPtrCollection<ColorBgra32> sourceRows = sourceRegion.Rows;
                RegionRowPtrCollection<ColorAlpha8> sourceMaskRows = sourceMaskRegion.Rows;

                SizeInt32 size = sourceRegion.Size;

                int sourcePointsSize = 0;

                for (int y = 0; y < size.Height; y++)
                {
                    ColorBgra32* src = sourceRows[y].Ptr;
                    ColorAlpha8* mask = sourceMaskRows[y].Ptr;

                    for (int x = 0; x < size.Width; x++)
                    {
                        if (*mask == ColorAlpha8.Opaque && src->A > 0)
                        {
                            sourcePointsSize++;
                        }

                        src++;
                        mask++;
                    }
                }

                ImmutableArray<Point2Int32>.Builder points = ImmutableArray.CreateBuilder<Point2Int32>(sourcePointsSize);

                if (sourcePointsSize > 0)
                {
                    for (int y = 0; y < size.Height; y++)
                    {
                        ColorBgra32* src = sourceRows[y].Ptr;
                        ColorAlpha8* mask = sourceMaskRows[y].Ptr;

                        for (int x = 0; x < size.Width; x++)
                        {
                            if (*mask == ColorAlpha8.Opaque && src->A > 0)
                            {
                                points.Add(new Point2Int32(x, y));
                            }

                            src++;
                            mask++;
                        }
                    }
                }

                this.sourcePoints = points.MoveToImmutable();
            }
        }

        private Point2Int32 RandomSourcePoint()
        {
            int index = this.random.Next(0, this.sourcePoints.Length);

            return this.sourcePoints[index];
        }

        private bool TryPoint(Point2Int32 point,
                              BettermentKind bettermentKind,
                              RegionPtr<ColorBgra32> sourceRegion,
                              RegionPtr<ColorAlpha8> sourceMaskRegion)
        {
            uint sum = 0;

            for (int i = 0; i < this.neighborCount; i++)
            {
                Point2Int32 offset = point.Add(this.neighbors[i].offset);

                if (ClippedOrMaskedSource(offset, sourceMaskRegion))
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
                    ColorBgra sourcePixel = sourceRegion[offset.X, offset.Y];
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

        private bool WrapOrClip(SizeInt32 size, ref Point2Int32 point)
        {
            while (point.X < 0)
            {
                if (this.parameters.TileHorizontal)
                {
                    point.X += size.Width;
                }
                else
                {
                    return false;
                }
            }

            while (point.X >= size.Width)
            {
                if (this.parameters.TileHorizontal)
                {
                    point.X -= size.Width;
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
                    point.Y += size.Height;
                }
                else
                {
                    return false;
                }
            }

            while (point.Y >= size.Height)
            {
                if (this.parameters.TileVertical)
                {
                    point.Y -= size.Height;
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
        private int? Synthesize(int pass,
                                Func<bool> abortCallback,
                                RegionPtr<ColorBgra32> sourceRegion,
                                RegionPtr<ColorAlpha8> sourceMaskRegion,
                                RegionPtr<ColorBgra32> targetRegion)
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

                Point2Int32 position = this.targetPoints[targetIndex];

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
                        Point2Int32 sourcePoint = neighbor.sourceOf.Subtract(neighbor.offset);

                        if (ClippedOrMaskedSource(sourcePoint, sourceMaskRegion))
                        {
                            continue;
                        }

                        if (this.tried[sourcePoint] == targetIndex)
                        {
                            continue;
                        }

                        perfectMatch = TryPoint(sourcePoint,
                                                BettermentKind.NeighborSource,
                                                sourceRegion,
                                                sourceMaskRegion);
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
                        perfectMatch = TryPoint(RandomSourcePoint(),
                                                BettermentKind.RandomSource,
                                                sourceRegion,
                                                sourceMaskRegion);
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

                        targetRegion[position] = sourceRegion[this.bestPoint];
                        this.sourceOf[position] = this.bestPoint;
                    }
                }
            }

            return repeatCountBetters;
        }

        private readonly struct Neighbor
        {
            public readonly ColorBgra pixel;
            public readonly Point2Int32 offset;
            public readonly Point2Int32 sourceOf;

            public Neighbor(ColorBgra pixel, Point2Int32 offset, Point2Int32 sourceOf)
            {
                this.pixel = pixel;
                this.offset = offset;
                this.sourceOf = sourceOf;
            }
        }

        private readonly struct RepetitionParameter
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
