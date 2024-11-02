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
*/

using PaintDotNet;
using PaintDotNet.Direct2D1;
using PaintDotNet.Effects;
using PaintDotNet.Imaging;
using PaintDotNet.Rendering;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ContentAwareFill
{
    internal sealed class ResynthesizerRunner : Disposable
    {
        private IBitmap<ColorAlpha8> sourceMask;
        private IBitmap<ColorAlpha8> targetMask;
        private int sampleSize;
        private SampleSource sampleFrom;
        private FillDirection fillDirection;
        private readonly IEffectEnvironment environment;
        private readonly IServiceProvider serviceProvider;
#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly IImagingFactory imagingFactory;
#pragma warning restore CA2213 // Disposable fields should be disposed

        /// <summary>
        /// Initializes a new instance of the <see cref="ResynthesizerRunner"/> class.
        /// </summary>
        /// <param name="environment">The source.</param>
        /// <param name="serviceProvider">The service provider.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="environment"/> is null.
        /// or
        /// <paramref name="serviceProvider"/> is null.
        /// </exception>
        public ResynthesizerRunner(IEffectEnvironment environment, IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(environment);
            ArgumentNullException.ThrowIfNull(serviceProvider);

            this.sourceMask = null;
            this.targetMask = null;
            this.sampleSize = 50;
            this.sampleFrom = SampleSource.Sides;
            this.fillDirection = FillDirection.InwardToCenter;
            this.environment = environment;
            this.serviceProvider = serviceProvider;
            this.imagingFactory = serviceProvider.GetService<IImagingFactory>();
        }

        /// <summary>
        /// Runs the Resynthesizer command.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progressCallback">The progress callback.</param>
        /// <returns>
        ///   The output if Resynthesizer completed successfully; otherwise, <see langword="null" /> if it was canceled.
        /// </returns>
        /// <exception cref="InvalidOperationException">Unsupported SampleFrom enumeration value.</exception>
        public IBitmap<ColorBgra32> Run(CancellationToken cancellationToken, Action<int> progressCallback = null)
        {
            IBitmap<ColorBgra32> output;

            RectInt32 expandedBounds = RenderSourceMask();
            RectInt32 originalBounds = this.environment.Selection.RenderBounds;

            if (this.targetMask is null)
            {
                this.targetMask = BitmapUtil.CreateFromBitmapSource(this.imagingFactory, this.environment.Selection.MaskBitmap);
            }

            SizeInt32 croppedSourceSize = this.sampleFrom switch
            {
                SampleSource.Sides => new SizeInt32(expandedBounds.X + expandedBounds.Width, originalBounds.Y + originalBounds.Height),
                SampleSource.TopAndBottom => new SizeInt32(originalBounds.X + originalBounds.Width, expandedBounds.Y + expandedBounds.Height),
                SampleSource.AllAround => new SizeInt32(expandedBounds.X + expandedBounds.Width, expandedBounds.Y + expandedBounds.Height),
                _ => throw new InvalidOperationException("Unsupported SampleFrom enumeration value: " + this.sampleFrom.ToString()),
            };

            MatchContextType matchContext = GetMatchContextType();

            try
            {
                using (Resynthesizer synth = new(matchContext,
                                                 this.environment.GetSourceBitmapBgra32(),
                                                 this.sourceMask,
                                                 expandedBounds,
                                                 croppedSourceSize,
                                                 this.targetMask,
                                                 cancellationToken,
                                                 progressCallback,
                                                 this.imagingFactory))
                {
                    synth.ContentAwareFill();
                    output = BitmapUtil.CreateFromBitmapSource(this.imagingFactory, synth.Target);
                }
            }
            catch (OperationCanceledException)
            {
                output = null;
            }

            return output;
        }

        /// <summary>
        /// Sets the parameters.
        /// </summary>
        /// <param name="sampleSize">The sample area size.</param>
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
                this.targetMask?.Dispose();
                this.sourceMask?.Dispose();
            }

            base.Dispose(disposing);
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

        private unsafe RectInt32 RenderSourceMask()
        {
            this.sourceMask ??= this.imagingFactory.CreateBitmap<ColorAlpha8>(this.environment.Document.Size);

            RectInt32 maskBounds = RectInt32.Empty;

            IDirect2DFactory d2dFactory = this.serviceProvider.GetService<IDirect2DFactory>();

            using (IDeviceContext deviceContext = d2dFactory.CreateBitmapDeviceContext(this.sourceMask))
            {
                // PrimitiveBlend.Copy is required for Direct2D to mask out the original selection.
                deviceContext.PrimitiveBlend = PrimitiveBlend.Copy;

                using (ISolidColorBrush selectedRegionBrush = deviceContext.CreateSolidColorBrush(SrgbColors.White))
                using (ISolidColorBrush unselectedRegionBrush = deviceContext.CreateSolidColorBrush(SrgbColors.Transparent))
                using (deviceContext.UseBeginDraw())
                {
                    IReadOnlyList<RectInt32> selectionRects = this.environment.Selection.RenderScans;
                    RectInt32 surfaceBounds = this.sourceMask.Bounds();

                    deviceContext.Clear(SrgbColors.Transparent);

                    foreach (RectInt32 rect in selectionRects)
                    {
                        RectInt32 expandedRect = RectInt32.Inflate(rect, this.sampleSize, this.sampleSize);
                        RectInt32 selectedArea = RectInt32.Intersect(expandedRect, surfaceBounds);

                        deviceContext.FillRectangle(selectedArea, selectedRegionBrush);

                        maskBounds = RectInt32.Union(maskBounds, selectedArea);
                    }

                    // Exclude the pixels in the original selection.
                    // This is does in a second pass because it would be overwritten by the enlarged scans
                    // when there is more than one scan rectangle.
                    foreach (RectInt32 rect in selectionRects)
                    {
                        deviceContext.FillRectangle(rect, unselectedRegionBrush);
                    }
                }
            }

            return maskBounds;
        }
    }
}
