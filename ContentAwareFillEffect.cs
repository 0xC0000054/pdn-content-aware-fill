/*
*  This file is part of pdn-content-aware-fill, A Resynthesizer-based
*  content aware fill Effect plug-in for Paint.NET.
*
*  Copyright (C) 2018 Nicholas Hayes
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
        private bool repeatEffect;

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
            repeatEffect = true;
        }

        public override EffectConfigDialog CreateConfigDialog()
        {
            repeatEffect = false;

            return new ContentAwareFillConfigDialog();
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
        internal static PdnRegion CreateSampleRegion(Rectangle surfaceBounds, PdnRegion selection, int sampleSize)
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
        internal static MatchContextType GetMatchContextType(SampleSource sampleFrom, FillDirection fillDirection)
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
        /// Creates a <see cref="MaskSurface"/> for the specified region.
        /// </summary>
        /// <param name="region">The region.</param>
        /// <param name="width">The width of the mask surface.</param>
        /// <param name="height">The height of the mask surface.</param>
        /// <returns>The mask for the specified region.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="width"/> is null.</exception>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        internal static unsafe MaskSurface CreateMask(PdnRegion region, int width, int height)
        {
            if (region == null)
            {
                throw new ArgumentNullException(nameof(region));
            }

            MaskSurface surface = new MaskSurface(width, height);

            Rectangle[] scans = region.GetRegionScansReadOnlyInt();

            for (int i = 0; i < scans.Length; i++)
            {
                Rectangle rect = scans[i];

                for (int y = rect.Top; y < rect.Bottom; y++)
                {
                    byte* ptr = surface.GetPointAddressUnchecked(rect.Left, y);

                    for (int x = rect.Left; x < rect.Right; x++)
                    {
                        *ptr = 255;
                        ptr++;
                    }
                }
            }

            return surface;
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "2")]
        protected override void OnSetRenderInfo(EffectConfigToken parameters, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            if (repeatEffect)
            {
                ContentAwareFillConfigToken token = (ContentAwareFillConfigToken)parameters;
                if (token.Destination != null)
                {
                    token.Destination.Dispose();
                    token.Destination = null;
                }

                Surface source = srcArgs.Surface;
                Rectangle sourceBounds = source.Bounds;
                PdnRegion selection = EnvironmentParameters.GetSelection(sourceBounds);

                using (PdnRegion sampleArea = CreateSampleRegion(sourceBounds, selection, token.SampleSize))
                using (MaskSurface sourceMask = CreateMask(sampleArea, source.Width, source.Height))
                using (MaskSurface destinationMask = CreateMask(selection, source.Width, source.Height))
                {
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
                        default:
                            croppedSourceSize = new Size(expandedBounds.X + expandedBounds.Width, expandedBounds.Y + expandedBounds.Height);
                            break;
                    }

                    ResynthesizerParameters resynthesizerParameters = new ResynthesizerParameters(false, false, matchContext, 0.0, 0.117, 16, 500);

                    using (Resynthesizer synth = new Resynthesizer(resynthesizerParameters, source, destinationMask, sourceMask, expandedBounds, croppedSourceSize, null))
                    {
                        try
                        {
                            if (synth.ContentAwareFill(() => IsCancelRequested))
                            {
                                token.Destination = synth.Target.Clone();
                            }
                        }
                        catch (ResynthizerException ex)
                        {
                            MessageBox.Show(ex.Message, Name, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, 0);
                        }
                    }
                }
            }

            base.OnSetRenderInfo(parameters, dstArgs, srcArgs);
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "2")]
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1")]
        public override void Render(EffectConfigToken parameters, RenderArgs dstArgs, RenderArgs srcArgs, Rectangle[] rois, int startIndex, int length)
        {
            ContentAwareFillConfigToken token = (ContentAwareFillConfigToken)parameters;

            if (token.Destination != null)
            {
                dstArgs.Surface.CopySurface(token.Destination, rois, startIndex, length);
            }
            else
            {
                dstArgs.Surface.CopySurface(srcArgs.Surface, rois, startIndex, length);
            }
        }
    }
}
