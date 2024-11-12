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
using PaintDotNet.AppModel;
using PaintDotNet.Effects;
using PaintDotNet.Imaging;
using PaintDotNet.Rendering;
using System;
using System.Drawing;

namespace ContentAwareFill
{
    [PluginSupportInfo(typeof(PluginSupportInfo))]
    public sealed class ContentAwareFillEffect : BitmapEffect<ContentAwareFillConfigToken>
    {
        private bool repeatEffect;
#pragma warning disable CA2213 // Disposable fields should be disposed
        private IBitmap<ColorBgra32> output;
        private IBitmapSource<ColorBgra32> sourceBitmap;
#pragma warning restore CA2213 // Disposable fields should be disposed

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

        public ContentAwareFillEffect() : base(StaticName, StaticImage, "Selection", BitmapEffectOptions.Create() with { IsConfigurable = true })
        {
            this.repeatEffect = true;
        }

        protected override IEffectConfigForm OnCreateConfigForm()
        {
            this.repeatEffect = false;

            return new ContentAwareFillConfigDialog();
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                DisposableUtil.Free(ref this.output);
                DisposableUtil.Free(ref this.sourceBitmap);
            }

            base.OnDispose(disposing);
        }

        /// <summary>
        /// Determines whether the whole image is selected.
        /// </summary>
        /// <param name="environment">The effect environment.</param>
        /// <returns>
        ///   <c>true</c> if the whole image is selected; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="environment"/> is null.</exception>
        internal static bool IsWholeImageSelected(IEffectEnvironment environment)
        {
            ArgumentNullException.ThrowIfNull(environment);

            RectInt32 documentBounds = new(Point2Int32.Zero, environment.Document.Size);

            bool wholeImageSelected = environment.Selection.RenderBounds == documentBounds;

            if (wholeImageSelected)
            {
                int imageWidth = documentBounds.Width;

                foreach (RectInt32 scan in environment.Selection.RenderScans)
                {
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

        protected override void OnSetToken(ContentAwareFillConfigToken token)
        {
            if (this.repeatEffect)
            {
                if (token.Output != null)
                {
                    token.Output.Dispose();
                    token.Output = null;
                }

                // This plugin does not support processing a selection of the whole image, it needs some unselected pixels
                // to replace the contents of the selected area.
                // When there is no active selection Paint.NET acts as if the whole image/layer is selected.
                if (!IsWholeImageSelected(this.Environment))
                {
                    try
                    {
                        using (ResynthesizerRunner resynthesizer = new(this.Environment, this.Services))
                        {
                            resynthesizer.SetParameters(token.SampleSize,
                                                        token.SampleFrom,
                                                        token.FillDirection,
                                                        token.Seed);

                            token.Output = resynthesizer.Run(this.CancellationToken);
                        }
                    }
                    catch (ResynthesizerException ex)
                    {
                        this.Services.GetService<IExceptionDialogService>().ShowErrorDialog(null, ex);
                    }
                }
            }

            if (token.Output != null)
            {
                this.output ??= this.Environment.ImagingFactory.CreateBitmap<ColorBgra32>(this.Environment.Document.Size);
                this.output = token.Output.ToBitmap();
            }
            else
            {
                this.sourceBitmap ??= this.Environment.GetSourceBitmapBgra32();
            }

            base.OnSetToken(token);
        }

        protected override unsafe void OnRender(IBitmapEffectOutput output)
        {
            if (this.output != null)
            {
                using (IBitmapLock<ColorBgra32> src = this.output.Lock(output.Bounds, BitmapLockOptions.Read))
                using (IBitmapLock<ColorBgra32> dst = output.LockBgra32())
                {
                    src.AsRegionPtr().CopyTo(dst.AsRegionPtr());
                }
            }
            else
            {
                using (IBitmapLock<ColorBgra32> dst = output.LockBgra32())
                {
                    this.sourceBitmap.CopyPixels(dst.Buffer, dst.BufferStride, dst.BufferSize, output.Bounds);
                }
            }
        }
    }
}
