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
*/

using PaintDotNet.Imaging;
using PaintDotNet.Rendering;
using System;
using System.Runtime.InteropServices;

namespace ContentAwareFill
{
    internal static class BitmapUtil
    {
        internal static unsafe IBitmap<TPixel> CreateFromBitmapSource<TPixel>(IImagingFactory imagingFactory,
                                                                              IBitmapSource<TPixel> source,
                                                                              SizeInt32? size = null,
                                                                              RectInt32? sourceRoi = null,
                                                                              bool clear = false) where TPixel : unmanaged, INaturalPixelInfo

        {
            IBitmap<TPixel> bitmap = imagingFactory.CreateBitmap<TPixel>(size ?? source.Size);

            CopyFromBitmapSource(source, bitmap, sourceRoi, clear);

            return bitmap;
        }

        internal static unsafe void CopyFromBitmapSource<TPixel>(IBitmapSource<TPixel> source,
                                                                 IBitmap<TPixel> destination,
                                                                 RectInt32? sourceRoi = null,
                                                                 bool clear = false) where TPixel : unmanaged, INaturalPixelInfo
        {
            using (IBitmapLock<TPixel> bitmapLock = destination.Lock(BitmapLockOptions.Write))
            {
                if (clear)
                {
                    NativeMemory.Clear(bitmapLock.Buffer, bitmapLock.BufferSize);
                }

                RectInt32? srcRect = null;

                if (sourceRoi.HasValue)
                {
                    RectInt32 roi = sourceRoi.Value;
                    SizeInt32 size = destination.Size;

                    int copyWidth = Math.Min(size.Width, roi.Width);
                    int copyHeight = Math.Min(size.Height, roi.Height);

                    srcRect = new RectInt32(roi.Location, copyWidth, copyHeight);
                }

                source.CopyPixels(bitmapLock.Buffer, bitmapLock.BufferStride, bitmapLock.BufferSize, srcRect);
            }
        }
    }
}
