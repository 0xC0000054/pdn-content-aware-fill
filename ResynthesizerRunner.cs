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
*/

using PaintDotNet;
using System;
using System.Drawing;

namespace ContentAwareFill
{
    internal sealed class ResynthesizerRunner : Disposable
    {
        private MaskSurface sourceMask;
        private MaskSurface destinationMask;
        private int sampleSize;
        private SampleSource sampleFrom;
        private FillDirection fillDirection;
        private readonly Surface sourceSurface;
        private readonly Rectangle sourceBounds;
        private readonly PdnRegion selection;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResynthesizerRunner"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="sourceBounds">The source bounds.</param>
        /// <param name="selection">The selection.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source"/> is null.
        /// or
        /// <paramref name="selection"/> is null.
        /// </exception>
        public ResynthesizerRunner(Surface source,
                                        Rectangle sourceBounds,
                                        PdnRegion selection)
        {
            this.sourceMask = null;
            this.destinationMask = null;
            this.sampleSize = 50;
            this.sampleFrom = SampleSource.Sides;
            this.fillDirection = FillDirection.InwardToCenter;
            this.sourceSurface = source ?? throw new ArgumentNullException(nameof(source));
            this.sourceBounds = sourceBounds;
            this.selection = selection ?? throw new ArgumentNullException(nameof(selection));
        }

        /// <summary>
        /// Runs the Resynthesizer command.
        /// </summary>
        /// <param name="abortCallback">The abort callback.</param>
        /// <param name="progressCallback">The progress callback.</param>
        /// <returns>
        ///   The output if Resynthesizer completed successfully; otherwise, <see langword="null" /> if it was canceled.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="abortCallback"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Unsupported SampleFrom enumeration value.</exception>
        public Surface Run(Func<bool> abortCallback, Action<int> progressCallback = null)
        {
            if (abortCallback is null)
            {
                throw new ArgumentNullException(nameof(abortCallback));
            }

            Surface output = null;

            using (PdnRegion sampleArea = CreateSampleRegion())
            {
                RenderSourceMask(sampleArea);
                if (this.destinationMask is null)
                {
                    CreateDestinationMask();
                }

                MatchContextType matchContext = GetMatchContextType();

                Rectangle expandedBounds = sampleArea.GetBoundsInt();
                Rectangle originalBounds = this.selection.GetBoundsInt();

                Size croppedSourceSize;

                switch (this.sampleFrom)
                {
                    case SampleSource.Sides:
                        croppedSourceSize = new Size(expandedBounds.X + expandedBounds.Width, originalBounds.Y + originalBounds.Height);
                        break;
                    case SampleSource.TopAndBottom:
                        croppedSourceSize = new Size(originalBounds.X + originalBounds.Width, expandedBounds.Y + expandedBounds.Height);
                        break;
                    case SampleSource.AllAround:
                        croppedSourceSize = new Size(expandedBounds.X + expandedBounds.Width, expandedBounds.Y + expandedBounds.Height);
                        break;
                    default:
                        throw new InvalidOperationException("Unsupported SampleFrom enumeration value: " + this.sampleFrom.ToString());
                }

                ResynthesizerParameters resynthesizerParameters = new ResynthesizerParameters(false, false, matchContext, 0.0, 0.117, 16, 500);

                using (Resynthesizer synth = new Resynthesizer(resynthesizerParameters,
                                                               this.sourceSurface,
                                                               this.destinationMask,
                                                               this.sourceMask,
                                                               expandedBounds,
                                                               croppedSourceSize,
                                                               progressCallback))
                {
                    if (synth.ContentAwareFill(abortCallback))
                    {
                        output = synth.Target.Clone();
                    }
                }
            }

            return output;
        }

        /// <summary>
        /// Sets the parameters.
        /// </summary>
        /// <param name="sampleFrom">The area of the selection to sample.</param>
        /// <param name="fillDirection">The direction to fill the generated image.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="sampleSize"/> must be positive.
        /// or
        /// <paramref name="sampleFrom"/> is not a valid <see cref="SampleSource"/> value.
        /// or
        /// <paramref name="fillDirection"/> is not a valid <see cref="FillDirection"/> value.
        /// </exception>
        public void SetParameters(int sampleSize, SampleSource sampleFrom, FillDirection fillDirection)
        {
            if (sampleSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleSize), "Must be positive.");
            }

            if (sampleFrom < SampleSource.AllAround || sampleFrom > SampleSource.TopAndBottom)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleFrom));
            }

            if (fillDirection < FillDirection.Random || fillDirection > FillDirection.OutwardFromCenter)
            {
                throw new ArgumentOutOfRangeException(nameof(fillDirection));
            }

            this.sampleSize = sampleSize;
            this.sampleFrom = sampleFrom;
            this.fillDirection = fillDirection;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.destinationMask?.Dispose();
                this.sourceMask?.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Creates a <see cref="PdnRegion"/> representing the sample area.
        /// </summary>
        private PdnRegion CreateSampleRegion()
        {
            if (this.sampleSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(this.sampleSize), "The sample size cannot be negative.");
            }

            Rectangle[] expandedScans = this.selection.GetRegionScansInt();

            for (int i = 0; i < expandedScans.Length; i++)
            {
                // Expand each scan rectangle by the specified number of pixels.
                expandedScans[i].Inflate(this.sampleSize, this.sampleSize);
            }

            PdnRegion sampleRegion;

            using (PdnGraphicsPath path = new PdnGraphicsPath())
            {
                path.FillMode = System.Drawing.Drawing2D.FillMode.Winding;
                path.AddRectangles(expandedScans);

                sampleRegion = new PdnRegion(path);
            }

            // Clip the expanded region to the surface bounds and exclude the original selection.
            // Excluding the original selection prevents sampling from the area that is to be replaced.
            sampleRegion.Intersect(this.sourceBounds);
            sampleRegion.Exclude(this.selection);

            return sampleRegion;
        }

        /// <summary>
        /// Gets the <see cref="MatchContextType"/> for the specified sampling options.
        /// </summary>
        /// <returns>
        /// A <see cref="MatchContextType"/> for the specified sampling options
        /// </returns>
        private MatchContextType GetMatchContextType()
        {
            if (this.fillDirection == FillDirection.Random)
            {
                return MatchContextType.Random;
            }
            else if (this.fillDirection == FillDirection.InwardToCenter)
            {
                switch (this.sampleFrom)
                {
                    case SampleSource.AllAround:
                        return MatchContextType.InwardConcentric;
                    case SampleSource.Sides:
                        return MatchContextType.InwardHorizontal;
                    case SampleSource.TopAndBottom:
                        return MatchContextType.InwardVertical;
                }
            }
            else
            {
                switch (this.sampleFrom)
                {
                    case SampleSource.AllAround:
                        return MatchContextType.OutwardConcentric;
                    case SampleSource.Sides:
                        return MatchContextType.OutwardHorizontal;
                    case SampleSource.TopAndBottom:
                        return MatchContextType.OutwardVertical;
                }
            }

            return MatchContextType.None;
        }

        /// <summary>
        /// Creates a <see cref="MaskSurface"/> for the destination.
        /// </summary>
        /// <returns>The mask for the destination.</returns>
        private unsafe void CreateDestinationMask()
        {
            this.destinationMask = new MaskSurface(this.sourceSurface.Width, this.sourceSurface.Height);

            Rectangle[] scans = this.selection.GetRegionScansReadOnlyInt();

            for (int i = 0; i < scans.Length; i++)
            {
                Rectangle rect = scans[i];

                for (int y = rect.Top; y < rect.Bottom; y++)
                {
                    byte* ptr = this.destinationMask.GetPointAddressUnchecked(rect.Left, y);

                    for (int x = rect.Left; x < rect.Right; x++)
                    {
                        *ptr = 255;
                        ptr++;
                    }
                }
            }
        }

        private unsafe void RenderSourceMask(PdnRegion region)
        {
            if (this.sourceMask == null)
            {
                this.sourceMask = new MaskSurface(this.sourceSurface.Width, this.sourceSurface.Height);
            }

            this.sourceMask.Clear();

            Rectangle[] scans = region.GetRegionScansReadOnlyInt();

            for (int i = 0; i < scans.Length; i++)
            {
                Rectangle rect = scans[i];

                for (int y = rect.Top; y < rect.Bottom; y++)
                {
                    byte* ptr = this.sourceMask.GetPointAddressUnchecked(rect.Left, y);

                    for (int x = rect.Left; x < rect.Right; x++)
                    {
                        *ptr = 255;
                        ptr++;
                    }
                }
            }

#if DEBUG
            using (Bitmap image = new Bitmap(this.sourceMask.Width, this.sourceMask.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                System.Drawing.Imaging.BitmapData data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height),
                                                                        System.Drawing.Imaging.ImageLockMode.WriteOnly, image.PixelFormat);

                try
                {
                    for (int y = 0; y < image.Height; y++)
                    {
                        byte* src = this.sourceMask.GetRowAddressUnchecked(y);
                        byte* dst = (byte*)data.Scan0 + (y * data.Stride);

                        for (int x = 0; x < image.Width; x++)
                        {
                            dst[0] = dst[1] = dst[2] = *src;
                            dst[3] = 255;

                            src++;
                            dst += 4;
                        }
                    }
                }
                finally
                {
                    image.UnlockBits(data);
                }
            }
#endif
        }
    }
}
