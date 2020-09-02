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
*/

using PaintDotNet;
using PaintDotNet.Effects;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;

namespace ContentAwareFill
{
    [PluginSupportInfo(typeof(PluginSupportInfo))]
    public sealed class ContentAwareFillEffect : Effect
    {
        private ContentAwareFillConfigDialog configDialog;
        private Surface destination;
        private MaskSurface sourceMask;
        private MaskSurface destinationMask;
        private int lastSampleSize;

        internal static string StaticName
        {
            get
            {
                return "Content Aware Fill";
            }
        }

        internal static Bitmap StaticImage
        {
            get
            {
                return new Bitmap(typeof(ContentAwareFillEffect), "icon.png");
            }
        }

        public ContentAwareFillEffect() : base(StaticName, StaticImage, "Selection", EffectFlags.Configurable)
        {
            configDialog = null;
            destination = null;
            sourceMask = null;
            destinationMask = null;
            lastSampleSize = 0;
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                configDialog?.Dispose();
                destination?.Dispose();
                destinationMask?.Dispose();
                sourceMask?.Dispose();
            }

            base.OnDispose(disposing);
        }

        public override EffectConfigDialog CreateConfigDialog()
        {
            configDialog = new ContentAwareFillConfigDialog();

            return configDialog;
        }

        /// <summary>
        /// Determines whether the whole image is selected.
        /// </summary>
        /// <param name="selection">The selection.</param>
        /// <param name="imageBounds">The image bounds.</param>
        /// <returns>
        ///   <c>true</c> if the whole image is selected; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="selection"/> is null.</exception>
        internal static bool IsWholeImageSelected(PdnRegion selection, Rectangle imageBounds)
        {
            if (selection is null)
            {
                ExceptionUtil.ThrowArgumentNullException(nameof(selection));
            }

            bool wholeImageSelected = selection.GetBoundsInt() == imageBounds;

            if (wholeImageSelected)
            {
                Rectangle[] scans = selection.GetRegionScansReadOnlyInt();
                int imageWidth = imageBounds.Width;

                for (int i = 0; i < scans.Length; i++)
                {
                    Rectangle scan = scans[i];

                    // The scan rectangle height is not checked because Paint.NET
                    // may split a tall rectangle into smaller chunks.
                    if (scan.X > 0 || scan.Width < imageWidth)
                    {
                        // The rectangle does not span the entire width of the image.
                        wholeImageSelected = false;
                        break;
                    }
                }
            }

            return wholeImageSelected;
        }

        /// <summary>
        /// Creates a <see cref="PdnRegion"/> representing the sample area.
        /// </summary>
        /// <param name="surfaceBounds">The bounds of the source surface.</param>
        /// <param name="selection">The existing selection.</param>
        /// <param name="sampleSize">The size of the sample area in pixels.</param>
        /// <returns>A <see cref="PdnRegion"/> representing the sample area.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="selection"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="sampleSize"/> cannot be negative.</exception>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        private static PdnRegion CreateSampleRegion(Rectangle surfaceBounds, PdnRegion selection, int sampleSize)
        {
            if (selection == null)
            {
                throw new ArgumentNullException(nameof(selection));
            }
            if (sampleSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleSize), "The sample size cannot be negative.");
            }

            Rectangle[] expandedScans = selection.GetRegionScansInt();

            for (int i = 0; i < expandedScans.Length; i++)
            {
                // Expand each scan rectangle by the specified number of pixels.
                expandedScans[i].Inflate(sampleSize, sampleSize);
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
            sampleRegion.Intersect(surfaceBounds);
            sampleRegion.Exclude(selection);

            return sampleRegion;
        }

        /// <summary>
        /// Gets the <see cref="MatchContextType"/> for the specified sampling options.
        /// </summary>
        /// <param name="sampleFrom">The area of the selection to sample.</param>
        /// <param name="fillDirection">The direction to fill the generated image.</param>
        /// <returns>
        /// A <see cref="MatchContextType"/> for the specified sampling options
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="sampleFrom"/> is not a valid <see cref="SampleSource"/> value.
        /// or
        /// <paramref name="fillDirection"/> is not a valid <see cref="FillDirection"/> value.
        /// </exception>
        private static MatchContextType GetMatchContextType(SampleSource sampleFrom, FillDirection fillDirection)
        {
            if (sampleFrom < SampleSource.AllAround || sampleFrom > SampleSource.TopAndBottom)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleFrom));
            }
            if (fillDirection < FillDirection.Random || fillDirection > FillDirection.OutwardFromCenter)
            {
                throw new ArgumentOutOfRangeException(nameof(fillDirection));
            }

            if (fillDirection == FillDirection.Random)
            {
                return MatchContextType.Random;
            }
            else if (fillDirection == FillDirection.InwardToCenter)
            {
                switch (sampleFrom)
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
                switch (sampleFrom)
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
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        private unsafe void CreateDestinationMask()
        {
            Surface sourceSurface = EnvironmentParameters.SourceSurface;

            destinationMask = new MaskSurface(sourceSurface.Width, sourceSurface.Height);

            Rectangle[] scans = EnvironmentParameters.GetSelection(sourceSurface.Bounds).GetRegionScansReadOnlyInt();

            for (int i = 0; i < scans.Length; i++)
            {
                Rectangle rect = scans[i];

                for (int y = rect.Top; y < rect.Bottom; y++)
                {
                    byte* ptr = destinationMask.GetPointAddressUnchecked(rect.Left, y);

                    for (int x = rect.Left; x < rect.Right; x++)
                    {
                        *ptr = 255;
                        ptr++;
                    }
                }
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        private unsafe void RenderSourceMask(PdnRegion region)
        {
#pragma warning disable RCS1180 // Inline lazy initialization.
            if (sourceMask == null)
            {
                sourceMask = new MaskSurface(EnvironmentParameters.SourceSurface.Width, EnvironmentParameters.SourceSurface.Height);
            }
#pragma warning restore RCS1180 // Inline lazy initialization.

            sourceMask.Clear();

            Rectangle[] scans = region.GetRegionScansReadOnlyInt();

            for (int i = 0; i < scans.Length; i++)
            {
                Rectangle rect = scans[i];

                for (int y = rect.Top; y < rect.Bottom; y++)
                {
                    byte* ptr = sourceMask.GetPointAddressUnchecked(rect.Left, y);

                    for (int x = rect.Left; x < rect.Right; x++)
                    {
                        *ptr = 255;
                        ptr++;
                    }
                }
            }

#if DEBUG
            using (Bitmap image = new Bitmap(sourceMask.Width, sourceMask.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                System.Drawing.Imaging.BitmapData data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height),
                                                                        System.Drawing.Imaging.ImageLockMode.WriteOnly, image.PixelFormat);

                try
                {
                    for (int y = 0; y < image.Height; y++)
                    {
                        byte* src = sourceMask.GetRowAddressUnchecked(y);
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

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "2")]
        protected override void OnSetRenderInfo(EffectConfigToken parameters, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            ContentAwareFillConfigToken token = (ContentAwareFillConfigToken)parameters;

            Surface source = srcArgs.Surface;
            Rectangle sourceBounds = source.Bounds;
            PdnRegion selection = EnvironmentParameters.GetSelection(sourceBounds);

            // This plugin does not support processing a selection of the whole image, it needs some unselected pixels
            // to replace the contents of the selected area.
            // When there is no active selection Paint.NET acts as if the whole image/layer is selected.
            //
            // The configuration dialog will check that the selection bounds are valid when it shows its user interface.
            // If that check succeeds, any further checks can be skipped.
            if (configDialog != null && configDialog.SelectionBoundsAreValid || !IsWholeImageSelected(selection, source.Bounds))
            {
                if (destination != null)
                {
                    destination.Dispose();
                    destination = null;
                }

                using (PdnRegion sampleArea = CreateSampleRegion(sourceBounds, selection, token.SampleSize))
                {
                    RenderSourceMask(sampleArea);
                    if (destinationMask == null)
                    {
                        CreateDestinationMask();
                    }

                    SampleSource sampleFrom = token.SampleFrom;
                    FillDirection fillDirection = token.FillDirection;

                    MatchContextType matchContext = GetMatchContextType(sampleFrom, fillDirection);

                    Rectangle expandedBounds = sampleArea.GetBoundsInt();
                    Rectangle originalBounds = selection.GetBoundsInt();

                    Size croppedSourceSize;

                    switch (sampleFrom)
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
                            throw new InvalidOperationException("Unsupported SampleFrom enumeration value: " + sampleFrom.ToString());
                    }

                    ResynthesizerParameters resynthesizerParameters = new ResynthesizerParameters(false, false, matchContext, 0.0, 0.117, 16, 500);

                    using (Resynthesizer synth = new Resynthesizer(resynthesizerParameters, source, destinationMask, sourceMask, expandedBounds,
                        croppedSourceSize, configDialog != null ? (Action<int>)configDialog.UpdateProgress : null))
                    {
                        try
                        {
                            if (synth.ContentAwareFill(() => IsCancelRequested))
                            {
                                destination = synth.Target.Clone();
                            }
                        }
                        catch (ResynthizerException ex)
                        {
                            if (configDialog != null)
                            {
                                // Only show an error message when the sample size changes.
                                if (token.SampleSize != lastSampleSize)
                                {
                                    configDialog.HandleError(ex);
                                }
                            }
                            else
                            {
                                MessageBox.Show(ex.Message, Name, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, 0);
                            }
                        }
                    }
                }

                if (configDialog != null)
                {
                    lastSampleSize = token.SampleSize;

                    // Reset the configuration dialog progress bar to 0.
                    configDialog.UpdateProgress(0);
                }
            }

            base.OnSetRenderInfo(parameters, dstArgs, srcArgs);
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "2")]
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1")]
        public override void Render(EffectConfigToken parameters, RenderArgs dstArgs, RenderArgs srcArgs, Rectangle[] rois, int startIndex, int length)
        {
            if (destination != null)
            {
                dstArgs.Surface.CopySurface(destination, rois, startIndex, length);
            }
            else
            {
                dstArgs.Surface.CopySurface(srcArgs.Surface, rois, startIndex, length);
            }
        }
    }
}
