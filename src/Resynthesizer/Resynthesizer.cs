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
using ContentAwareFill.Collections;
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.Imaging;
using PaintDotNet.Rendering;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace ContentAwareFill
{
    internal sealed class Resynthesizer : Disposable
    {
        private const int ColorChannelCount = 3;
        private const int Neighbors = 16;
        private const double SensitivityToOutliers = 0.117;
        private const int Trys = 500;

        private static readonly ImmutableArray<ushort> DiffTable = MakeDiffTable();

        private readonly CancellationToken cancellationToken;
        private readonly Action<int> progressCallback;
        private readonly object progressSync;

#pragma warning disable IDE0032 // Use auto property
        private IBitmap<ColorBgra32> target;
#pragma warning restore IDE0032 // Use auto property
        private IBitmap<ColorBgra32> source;
        private IBitmap<ColorAlpha8> targetMask;
        private IBitmap<ColorAlpha8> sourceMask;

        private readonly MatchContextType matchContext;
        private readonly SizeInt32 targetSize;
        private readonly RepetitionParameter[] repetitionParameters;
        private PointIndexedArray<int> tried;
        private readonly PointIndexedBitArray hasValue;
        private PointIndexedArray<Point2Int32> sourceOf;
        private ImmutableArray<Point2Int32> sortedOffsets;
        private ImmutablePooledList<Point2Int32> targetPoints;
        private ImmutablePooledList<Point2Int32> sourcePoints;
        private int targetTriesCount;
        private int totalTargets;

        /// <summary>
        /// Initializes a new instance of the <see cref="Resynthesizer"/> class.
        /// </summary>
        /// <param name="matchContext">The match context.</param>
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
        public Resynthesizer(MatchContextType matchContext,
                             IEffectInputBitmap<ColorBgra32> source,
                             IBitmap<ColorAlpha8> sourceMask,
                             RectInt32 sourceRoi,
                             SizeInt32 croppedSourceSize,
                             IBitmap<ColorAlpha8> targetMask,
                             Action<int> progressCallback,
                             IImagingFactory imagingFactory,
                             CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(sourceMask);
            ArgumentNullException.ThrowIfNull(targetMask);
            ArgumentNullException.ThrowIfNull(imagingFactory);

            if (targetMask.Size != source.Size)
            {
                throw new ArgumentException("The target mask size does not match the source image.");
            }

            this.matchContext = matchContext;
            this.target = source.ToBitmap();
            this.targetSize = this.target.Size;
            this.source = BitmapUtil.CreateFromBitmap(imagingFactory, source, croppedSourceSize, sourceRoi);
            this.sourceMask = BitmapUtil.CreateFromBitmap(imagingFactory, sourceMask, croppedSourceSize, sourceRoi, clear: true);
            this.targetMask = targetMask.CreateRefT();

            this.repetitionParameters = new RepetitionParameter[ResynthesizerConstants.MaxPasses];
            this.tried = new PointIndexedArray<int>(this.targetSize, -1, cancellationToken);
            this.hasValue = PointIndexedBitArray.CreateFalse(this.targetSize);
            this.sourceOf = new PointIndexedArray<Point2Int32>(this.targetSize, new Point2Int32(-1, -1), cancellationToken);
            this.cancellationToken = cancellationToken;
            this.progressCallback = progressCallback;
            this.progressSync = new object();
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

        /// <summary>
        /// Performs the content the aware fill.
        /// </summary>
        /// <exception cref="OperationCanceledException">The operation has been canceled.</exception>
        /// <exception cref="ResynthesizerException">
        /// The source selection is empty.
        /// or
        /// The destination layer is empty.
        /// </exception>
        public void ContentAwareFill()
        {
            PrepareTargetPoints();
            // The constructor handles the setup performed by prepare_target_sources.
            PrepareSourcePoints();

            PrepareSortedOffsets();
            // The constructor handles the setup performed by prepare_tried.
            PrepareRepetitionParameters();

            using (IBitmapLock<ColorBgra32> sourceLock = this.source.Lock(BitmapLockOptions.Read))
            using (IBitmapLock<ColorAlpha8> sourceMaskLock = this.sourceMask.Lock(BitmapLockOptions.Read))
            using (IBitmapLock<ColorBgra32> targetLock = this.target.Lock(BitmapLockOptions.ReadWrite))
            {
                try
                {
                    RegionPtr<ColorBgra32> sourceRegion = sourceLock.AsRegionPtr();
                    RegionPtr<ColorAlpha8> sourceMaskRegion = sourceMaskLock.AsRegionPtr();
                    RegionPtr<ColorBgra32> targetRegion = targetLock.AsRegionPtr();

                    uint betters = 0;

                    int maxThreadCount = Environment.ProcessorCount;

                    Task<uint>[] tasks = new Task<uint>[maxThreadCount];

                    for (int i = 0; i < ResynthesizerConstants.MaxPasses; i++)
                    {
                        int endIndex = this.repetitionParameters[i].end;

                        for (int threadIndex = 0; threadIndex < maxThreadCount; threadIndex++)
                        {
                            tasks[threadIndex] = Task<uint>.Factory.StartNew(Synthesize,
                                                                  new SynthesizeThreadState(sourceRegion,
                                                                                            sourceMaskRegion,
                                                                                            targetRegion,
                                                                                            threadIndex,
                                                                                            endIndex,
                                                                                            maxThreadCount),
                                                                  this.cancellationToken);
                        }

                        for (int threadIndex = 0; threadIndex < maxThreadCount; threadIndex++)
                        {
                            betters += tasks[threadIndex].Result;
                        }

                        if (((float)betters / this.targetPoints.Count) < ResynthesizerConstants.TerminateFraction)
                        {
                            break;
                        }
                    }
                }
                catch (AggregateException ex)
                {
                    var exceptions = ex.InnerExceptions;

                    if (exceptions.Count == 1)
                    {
                        Exception exception = exceptions[0];

                        if (exception is OperationCanceledException)
                        {
                            ExceptionDispatchInfo.Throw(exception);
                        }
                        else
                        {
                            throw new ResynthesizerException(exception.Message, exception);
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (TaskCanceledException)
                {
                    throw new OperationCanceledException();
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposableUtil.Free(ref this.target);
                DisposableUtil.Free(ref this.source);
                DisposableUtil.Free(ref this.targetMask);
                DisposableUtil.Free(ref this.sourceMask);
                DisposableUtil.Free(ref this.tried);
                DisposableUtil.Free(ref this.sourceOf);
            }

                base.Dispose(disposing);
        }

        private static bool ClippedOrMaskedSource(Point2Int32 point, RegionPtr<ColorAlpha8> sourceMaskRegion)
        {
            return point.X < 0 ||
                   point.Y < 0 ||
                   point.X >= sourceMaskRegion.Width ||
                   point.Y >= sourceMaskRegion.Height ||
                   sourceMaskRegion[point.X, point.Y] != ColorAlpha8.Opaque;
        }

        private static ImmutableArray<ushort> MakeDiffTable()
        {
            ImmutableArray<ushort>.Builder diffTable = ImmutableArray.CreateBuilder<ushort>(512);

            double valueDivisor = NegLogCauchy(1.0 / SensitivityToOutliers);

            for (int i = -256; i < 256; i++)
            {
                double value = NegLogCauchy(i / 256.0 / SensitivityToOutliers) / valueDivisor * ResynthesizerConstants.MaxWeight;

                diffTable.Add((ushort)value);
                // The original code populated a map diff table array that is used to when mapping between images.
                // This is not required for the content aware fill functionality.
            }

            return diffTable.MoveToImmutable();
        }

        private static double NegLogCauchy(double d)
        {
            return Math.Log((d * d) + 1.0);
        }

        private int PrepareNeighbors(
            RegionPtr<ColorBgra32> targetRegion,
            Point2Int32 position,
            Span<Neighbor> neighbors)
        {
            int neighborCount = 0;

            for (int i = 0; i < this.sortedOffsets.Length; i++)
            {
                this.cancellationToken.ThrowIfCancellationRequested();

                Point2Int32 offset = this.sortedOffsets[i];
                Point2Int32 neighborPoint = position.Add(offset);

                if (InTargetBounds(neighborPoint) && this.hasValue[neighborPoint])
                {
                    neighbors[neighborCount] = new Neighbor(targetRegion[neighborPoint.X, neighborPoint.Y],
                                                            offset,
                                                            this.sourceOf[neighborPoint]);
                    neighborCount++;

                    if (neighborCount >= Neighbors)
                    {
                        break;
                    }
                }
            }

            return neighborCount;
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

        /// <summary>
        /// Prepares the source points.
        /// </summary>
        /// <exception cref="ResynthesizerException">The source region is empty.</exception>
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

                ulong sourcePointsSize = 0;

                for (int y = 0; y < size.Height; y++)
                {
                    this.cancellationToken.ThrowIfCancellationRequested();

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

                if (sourcePointsSize > 0)
                {
                    PooledList<Point2Int32> points = new(checked((int)sourcePointsSize));

                    for (int y = 0; y < size.Height; y++)
                    {
                        this.cancellationToken.ThrowIfCancellationRequested();

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

                    this.sourcePoints = new ImmutablePooledList<Point2Int32>(points);
                }
                else
                {
                    throw new ResynthesizerException(Properties.Resources.SourcePointsEmpty);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void PrepareSortedOffsets()
        {
            SizeInt32 sourceSize = this.source.Size;

            int width = sourceSize.Width;
            int height = sourceSize.Height;

            List<List<Point2Int32>> offsetsByDistance = new();

            for (int y = -height + 1; y < height; y++)
            {
                this.cancellationToken.ThrowIfCancellationRequested();

                for (int x = -width + 1; x < width; x++)
                {
                    Point2Int32 point = new(x, y);
                    int distance = (point.X * point.X) + (point.Y * point.Y); // can't be negative
                    EnsureCount(offsetsByDistance, distance + 1);
                    offsetsByDistance[distance] ??= new();
                    offsetsByDistance[distance]!.Add(point);
                }
            }

            this.sortedOffsets = offsetsByDistance
                .Where(l => l is not null)
                .SelectMany(s => s)
                .ToImmutableArray();
            this.cancellationToken.ThrowIfCancellationRequested();

            static void EnsureCount<T>(IList<T> list, int count)
            {
                while (list.Count < count)
                {
                    list.Add(default);
                }
            }
        }

        /// <summary>
        /// Prepares the target points.
        /// </summary>
        /// <exception cref="ResynthesizerException">The target points are empty.</exception>
        private unsafe void PrepareTargetPoints()
        {
            using (IBitmapLock<ColorAlpha8> targetMaskLock = this.targetMask.Lock(BitmapLockOptions.Read))
            {
                RegionPtr<ColorAlpha8> targetMaskRegion = targetMaskLock.AsRegionPtr();

                ulong targetPointsSize = 0;

                SizeInt32 targetSize = this.target.Size;

                foreach (RegionRowPtr<ColorAlpha8> row in targetMaskRegion.Rows)
                {
                    this.cancellationToken.ThrowIfCancellationRequested();

                    foreach (ColorAlpha8 pixel in row)
                    {
                        if (pixel != ColorAlpha8.Transparent)
                        {
                            targetPointsSize++;
                        }
                    }
                }

                if (targetPointsSize > 0)
                {
                    PooledList<Point2Int32> points = new(checked((int)targetPointsSize));
                    bool useContext = this.matchContext != MatchContextType.None;

                    using (IBitmapLock<ColorBgra32> targetLock = this.target.Lock(BitmapLockOptions.Read))
                    {
                        RegionPtr<ColorBgra32> targetRegion = targetLock.AsRegionPtr();
                        RegionRowPtrCollection<ColorBgra32> targetRows = targetRegion.Rows;
                        RegionRowPtrCollection<ColorAlpha8> targetMaskRows = targetMaskRegion.Rows;

                        for (int y = 0; y < targetSize.Height; y++)
                        {
                            this.cancellationToken.ThrowIfCancellationRequested();

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

                    TargetPointSorter.Sort(points, this.matchContext);
                    this.cancellationToken.ThrowIfCancellationRequested();
                    this.targetPoints = new ImmutablePooledList<Point2Int32>(points);
                }
                else
                {
                    throw new ResynthesizerException(Properties.Resources.TargetPointsEmpty);
                }
            }
        }

        private Point2Int32 RandomSourcePoint()
        {
            int index = ResynthesizerRandom.ThreadInstance.Next(0, this.sourcePoints.Count);

            return this.sourcePoints[index];
        }

        /// <summary>
        /// The core function that performs the image synthesis.
        /// </summary>
        /// <param name="pass">The pass.</param>
        /// <param name="sourceRegion">The source region.</param>
        /// <param name="sourceMaskRegion">The source mask region.</param>
        /// <param name="targetRegion">The target region.</param>
        /// <returns>The match count of the image synthesis.</returns>
        /// <exception cref="OperationCanceledException">The operation has been canceled.</exception>
        private uint Synthesize(object threadState)
        {
            SynthesizeThreadState state = (SynthesizeThreadState)threadState;

            RegionPtr<ColorBgra32> sourceRegion = state.sourceRegion;
            RegionPtr< ColorAlpha8 > sourceMaskRegion = state.sourceMaskRegion;
            RegionPtr<ColorBgra32> targetRegion = state.targetRegion;
            int targetStartIndex = state.startIndex;
            int targetEndIndex = state.endIndex;
            int threadCount = state.threadCount;

            uint repeatCountBetters = 0;
            bool perfectMatch;

            Span<Neighbor> neighbors = stackalloc Neighbor[Neighbors];

            for (int targetIndex = targetStartIndex; targetIndex < targetEndIndex; targetIndex += threadCount)
            {
                this.cancellationToken.ThrowIfCancellationRequested();

                // Report progress in increments of 4096.
                if ((targetIndex & 4095) == 0 && this.progressCallback != null)
                {
                    lock (this.progressSync)
                    {
                        this.targetTriesCount += 4096;

                        double value = ((double)this.targetTriesCount / (double)this.totalTargets) * 100.0;

                        this.progressCallback((int)value.Clamp(0.0, 100.0));
                    }
                }

                Point2Int32 position = this.targetPoints[targetIndex];

                this.hasValue[position] = true;

                int neighborCount = PrepareNeighbors(targetRegion, position, neighbors);

                uint best = uint.MaxValue;
                BettermentKind latestBettermentKind = BettermentKind.None;
                Point2Int32 bestPoint = Point2Int32.Zero;
                perfectMatch = false;

                for (int neighborIndex = 0; neighborIndex < neighborCount; neighborIndex++)
                {
                    this.cancellationToken.ThrowIfCancellationRequested();

                    Neighbor neighbor = neighbors[neighborIndex];
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

                        if (latestBettermentKind == BettermentKind.None)
                        {
                            latestBettermentKind = BettermentKind.NeighborSource;
                        }

                        perfectMatch = TryPoint(sourcePoint,
                                                sourceRegion,
                                                sourceMaskRegion,
                                                ref best,
                                                ref bestPoint,
                                                neighborCount,
                                                neighbors);
                        if (perfectMatch)
                        {
                            latestBettermentKind = BettermentKind.PerfectMatch;
                            break;
                        }

                        this.tried[sourcePoint] = targetIndex;
                    }
                }

                if (!perfectMatch)
                {
                    latestBettermentKind = BettermentKind.RandomSource;

                    for (uint i = 0; i < Trys; i++)
                    {
                        this.cancellationToken.ThrowIfCancellationRequested();

                        perfectMatch = TryPoint(RandomSourcePoint(),
                                                sourceRegion,
                                                sourceMaskRegion,
                                                ref best,
                                                ref bestPoint,
                                                neighborCount,
                                                neighbors);
                        if (perfectMatch)
                        {
                            latestBettermentKind = BettermentKind.PerfectMatch;
                            break;
                        }
                    }
                }

                if (latestBettermentKind != BettermentKind.None)
                {
                    if (this.sourceOf[position] != bestPoint)
                    {
                        repeatCountBetters++;

                        targetRegion[position] = sourceRegion[bestPoint];
                        this.sourceOf[position] = bestPoint;
                    }
                }
            }

            return repeatCountBetters;
        }

        private bool TryPoint(Point2Int32 point,
                              RegionPtr<ColorBgra32> sourceRegion,
                              RegionPtr<ColorAlpha8> sourceMaskRegion,
                              ref uint best,
                              ref Point2Int32 bestPoint,
                              int neighborCount,
                              ReadOnlySpan<Neighbor> neighbors)
        {
            uint sum = 0;

            for (int i = 0; i < neighborCount; i++)
            {
                this.cancellationToken.ThrowIfCancellationRequested();

                Point2Int32 offset = point.Add(neighbors[i].offset);

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
                    ColorBgra32 sourcePixel = sourceRegion[offset.X, offset.Y];
                    ColorBgra32 targetPixel = neighbors[i].pixel;

                    if (i > 0)
                    {
                        sum += DiffTable[256 + targetPixel.B - sourcePixel.B];
                        sum += DiffTable[256 + targetPixel.G - sourcePixel.G];
                        sum += DiffTable[256 + targetPixel.R - sourcePixel.R];
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
            bestPoint = point;

            return sum <= 0;
        }

        private bool InTargetBounds(Point2Int32 point)
        {
            return point.X >= 0
                && point.X < this.targetSize.Width
                && point.Y >= 0
                && point.Y < this.targetSize.Height;
        }

        private readonly struct Neighbor
        {
            public readonly ColorBgra32 pixel;
            public readonly Point2Int32 offset;
            public readonly Point2Int32 sourceOf;

            public Neighbor(ColorBgra32 pixel, Point2Int32 offset, Point2Int32 sourceOf)
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

        private sealed class SynthesizeThreadState
        {
            public readonly RegionPtr<ColorBgra32> sourceRegion;
            public readonly RegionPtr<ColorAlpha8> sourceMaskRegion;
            public readonly RegionPtr<ColorBgra32> targetRegion;
            public readonly int startIndex;
            public readonly int endIndex;
            public readonly int threadCount;

            public SynthesizeThreadState(RegionPtr<ColorBgra32> sourceRegion,
                                         RegionPtr<ColorAlpha8> sourceMaskRegion,
                                         RegionPtr<ColorBgra32> targetRegion,
                                         int startIndex,
                                         int endIndex,
                                         int threadCount)
            {
                this.sourceRegion = sourceRegion;
                this.sourceMaskRegion = sourceMaskRegion;
                this.targetRegion = targetRegion;
                this.startIndex = startIndex;
                this.endIndex = endIndex;
                this.threadCount = threadCount;
            }
        }
    }
}
