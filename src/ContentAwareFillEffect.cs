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
*/

using PaintDotNet;
using PaintDotNet.Effects;
using System;
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
                return new Bitmap(typeof(ContentAwareFillEffect), PluginIconUtil.GetIconResourceForDpi(UIScaleFactor.Current.Dpi));
            }
        }

        public ContentAwareFillEffect() : base(StaticName, StaticImage, "Selection", new EffectOptions { Flags = EffectFlags.Configurable })
        {
            this.repeatEffect = true;
        }

        public override EffectConfigDialog CreateConfigDialog()
        {
            this.repeatEffect = false;

            return new ContentAwareFillConfigDialog();
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

        protected override void OnSetRenderInfo(EffectConfigToken parameters, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            if (this.repeatEffect)
            {
                ContentAwareFillConfigToken token = (ContentAwareFillConfigToken)parameters;

                if (token.Output != null)
                {
                    token.Output.Dispose();
                    token.Output = null;
                }

                Surface source = srcArgs.Surface;
                Rectangle sourceBounds = source.Bounds;
                PdnRegion selection = this.EnvironmentParameters.GetSelectionAsPdnRegion();

                // This plugin does not support processing a selection of the whole image, it needs some unselected pixels
                // to replace the contents of the selected area.
                // When there is no active selection Paint.NET acts as if the whole image/layer is selected.
                if (!IsWholeImageSelected(selection, source.Bounds))
                {
                    try
                    {
                        using (ResynthesizerRunner resynthesizer = new(source, sourceBounds, selection))
                        {
                            resynthesizer.SetParameters(token.SampleSize, token.SampleFrom, token.FillDirection);

                            token.Output = resynthesizer.Run(() => this.IsCancelRequested);
                        }
                    }
                    catch (ResynthizerException ex)
                    {
                        MessageBox.Show(ex.Message, this.Name, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, 0);
                    }
                }
            }

            base.OnSetRenderInfo(parameters, dstArgs, srcArgs);
        }

        public override void Render(EffectConfigToken parameters, RenderArgs dstArgs, RenderArgs srcArgs, Rectangle[] rois, int startIndex, int length)
        {
            ContentAwareFillConfigToken token = (ContentAwareFillConfigToken)parameters;

            if (token.Output != null)
            {
                dstArgs.Surface.CopySurface(token.Output, rois, startIndex, length);
            }
            else
            {
                dstArgs.Surface.CopySurface(srcArgs.Surface, rois, startIndex, length);
            }
        }
    }
}
