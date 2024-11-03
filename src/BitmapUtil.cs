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
using PaintDotNet.Effects;
using PaintDotNet.Imaging;
using PaintDotNet.Rendering;
using System;

namespace ContentAwareFill
{
    internal static class BitmapUtil
    {
        internal static unsafe IBitmap<TPixel> CreateFromBitmap<TPixel>(IImagingFactory imagingFactory,
                                                                        IEffectInputBitmap<TPixel> source,
                                                                        SizeInt32 size,
                                                                        RectInt32 sourceRoi) where TPixel : unmanaged, INaturalPixelInfo

        {
            IBitmap<TPixel> destination = imagingFactory.CreateBitmap<TPixel>(size);

            using (IBitmapLock<TPixel> sourceLock = source.Lock(GetSourceRect(size, sourceRoi)))
            using (IBitmapLock<TPixel> destinationLock = destination.Lock(BitmapLockOptions.Write))
            {
                sourceLock.AsRegionPtr().CopyTo(destinationLock.AsRegionPtr());
            }

            return destination;
        }

        internal static unsafe IBitmap<TPixel> CreateFromBitmap<TPixel>(IImagingFactory imagingFactory,
                                                                        IBitmap<TPixel> source,
                                                                        SizeInt32 size,
                                                                        RectInt32 sourceRoi,
                                                                        bool clear = false) where TPixel : unmanaged, INaturalPixelInfo

        {
            IBitmap<TPixel> destination = imagingFactory.CreateBitmap<TPixel>(size);

            using (IBitmapLock<TPixel> sourceLock = source.Lock(GetSourceRect(size, sourceRoi), BitmapLockOptions.Read))
            using (IBitmapLock<TPixel> destinationLock = destination.Lock(BitmapLockOptions.Write))
            {
                RegionPtr<TPixel> sourceRegion = sourceLock.AsRegionPtr();
                RegionPtr<TPixel> destRegion = destinationLock.AsRegionPtr();

                if (clear)
                {
                    destRegion.Clear();
                }

                sourceRegion.CopyTo(destRegion);
            }

            return destination;
        }

        private static RectInt32 GetSourceRect(SizeInt32 destinationSize, RectInt32 sourceRoi)
        {
            int copyWidth = Math.Min(destinationSize.Width, sourceRoi.Width);
            int copyHeight = Math.Min(destinationSize.Height, sourceRoi.Height);

            return new(sourceRoi.Location, copyWidth, copyHeight);
        }
    }
}
