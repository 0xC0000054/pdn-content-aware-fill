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

// This file has been adapted from.
/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See License-pdn.txt for full licensing and attribution                      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Drawing;

namespace ContentAwareFill
{
    /// <summary>
    /// An 8-bit per pixel surface used for the selection mask.
    /// </summary>
    internal sealed class MaskSurface : IDisposable
    {
#pragma warning disable IDE0032 // Use auto property
        private int width;
        private int height;
        private int stride;
        private MemoryBlock scan0;
        private bool disposed;
#pragma warning restore IDE0032 // Use auto property

        /// <summary>
        /// Gets the width of the Surface.
        /// </summary>
        public int Width
        {
            get
            {
                return width;
            }
        }

        /// <summary>
        /// Gets the height of the Surface.
        /// </summary>
        public int Height
        {
            get
            {
                return height;
            }
        }

        public Rectangle Bounds
        {
            get
            {
                return new Rectangle(0, 0, width, height);
            }
        }

        public MaskSurface(int width, int height)
        {
            disposed = false;
            this.width = width;
            this.height = height;
            stride = width;
            scan0 = new MemoryBlock(width * height);
        }

        private MaskSurface(int width, int height, int stride, MemoryBlock scan0)
        {
            disposed = false;
            this.width = width;
            this.height = height;
            this.stride = stride;
            this.scan0 = scan0;
        }

        /// <summary>
        /// Sets the contents of this surface to zero.
        /// </summary>
        public unsafe void Clear()
        {
            Memory.SetToZero(scan0.VoidStar, (ulong)scan0.Length);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public MaskSurface Clone()
        {
            MaskSurface surface = new MaskSurface(width, height);
            surface.CopySurface(this);
            return surface;
        }

        /// <summary>
        /// Copies the contents of the given surface to the upper left corner of this surface.
        /// </summary>
        /// <param name="source">The surface to copy pixels from.</param>
        /// <remarks>
        /// The source surface does not need to have the same dimensions as this surface. Clipping
        /// will be handled automatically. No resizing will be done.
        /// </remarks>
        private void CopySurface(MaskSurface source)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("Surface");
            }

            if (stride == source.stride &&
                width == source.width &&
                height == source.height)
            {
                unsafe
                {
                    Memory.Copy(scan0.VoidStar,
                                source.scan0.VoidStar,
                                ((ulong)(height - 1) * (ulong)stride) + (ulong)width);
                }
            }
            else
            {
                int copyWidth = Math.Min(width, source.width);
                int copyHeight = Math.Min(height, source.height);

                unsafe
                {
                    for (int y = 0; y < copyHeight; ++y)
                    {
                        Memory.Copy(GetRowAddressUnchecked(y), source.GetRowAddressUnchecked(y), (ulong)copyWidth);
                    }
                }
            }
        }

        /// <summary>
        /// Copies the contents of the given surface to the upper left of this surface.
        /// </summary>
        /// <param name="source">The surface to copy pixels from.</param>
        /// <param name="sourceRoi">
        /// The region of the source to copy from. The upper left of this rectangle
        /// will be mapped to (0,0) on this surface.
        /// The source surface does not need to have the same dimensions as this surface. Clipping
        /// will be handled automatically. No resizing will be done.
        /// </param>
        public void CopySurface(MaskSurface source, Rectangle sourceRoi)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("Surface");
            }

            sourceRoi.Intersect(source.Bounds);
            int copiedWidth = Math.Min(width, sourceRoi.Width);
            int copiedHeight = Math.Min(Height, sourceRoi.Height);

            if (copiedWidth == 0 || copiedHeight == 0)
            {
                return;
            }

            using (MaskSurface src = source.CreateWindow(sourceRoi))
            {
                CopySurface(src);
            }
        }

        /// <summary>
        /// Creates a Surface that aliases a portion of this Surface.
        /// </summary>
        /// <param name="bounds">The portion of this Surface that will be aliased.</param>
        /// <remarks>The upper left corner of the new Surface will correspond to the
        /// upper left corner of this rectangle in the original Surface.</remarks>
        /// <returns>A Surface that aliases the requested portion of this Surface.</returns>
        public MaskSurface CreateWindow(Rectangle bounds)
        {
            return CreateWindow(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        public MaskSurface CreateWindow(int x, int y, int windowWidth, int windowHeight)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("Surface");
            }

            if (windowHeight == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(windowHeight), "must be greater than zero");
            }

            Rectangle original = Bounds;
            Rectangle sub = new Rectangle(x, y, windowWidth, windowHeight);
            Rectangle clipped = Rectangle.Intersect(original, sub);

            if (clipped != sub)
            {
                throw new ArgumentOutOfRangeException("bounds", new Rectangle(x, y, windowWidth, windowHeight),
                    "bounds parameters must be a subset of this Surface's bounds");
            }

            long offset = ((long)stride * (long)y) + ((long)x);
            long length = ((windowHeight - 1) * (long)stride) + (long)windowWidth;
            MemoryBlock block = new MemoryBlock(scan0, offset, length);

            return new MaskSurface(windowWidth, windowHeight, stride, block);
        }

        public byte GetPoint(int x, int y)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
            {
                throw new ArgumentOutOfRangeException("(x,y)", new Point(x, y), "Coordinates out of range, max=" + new Size(width - 1, height - 1).ToString());
            }

            return GetPointUnchecked(x, y);
        }

        public byte GetPointUnchecked(int x, int y)
        {
            unsafe
            {
               return *GetPointAddressUnchecked(x, y);
            }
        }

        public unsafe byte* GetPointAddressUnchecked(int x, int y)
        {
            return (byte*)scan0.VoidStar + (y * stride) + x;
        }

        public unsafe byte* GetRowAddressUnchecked(int y)
        {
            return (byte*)scan0.VoidStar + (y * stride);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed && disposing)
            {
                disposed = true;
                if (scan0 != null)
                {
                    scan0.Dispose();
                    scan0 = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
