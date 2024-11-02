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

using ContentAwareFill.Collections;
using PaintDotNet;
using PaintDotNet.Rendering;
using System;
using System.Threading;

namespace ContentAwareFill
{
    internal sealed unsafe class PointIndexedArray<T> : Disposable where T : unmanaged
    {
        private NativeArray<T> items;
        private readonly uint stride;

        public PointIndexedArray(SizeInt32 size, T defaultValue, CancellationToken cancellationToken)
        {
            this.stride = checked((uint)size.Width);
            uint height = checked((uint)size.Height);

            this.items = new((nuint)this.stride * height);

            for (uint y = 0; y < height; y++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                new Span<T>(this.items.GetAddress((nuint)y * this.stride), size.Width).Fill(defaultValue);
            }
        }

        public T this[Point2Int32 target]
        {
            get
            {
                return GetValue(target.X, target.Y);
            }
            set
            {
                SetValue(target.X, target.Y, value);
            }
        }

        public T GetValue(int x, int y)
        {
            nuint index = ((nuint)checked((uint)y) * this.stride) + checked((uint)x);

            return this.items[index];
        }

        public void SetValue(int x, int y, T value)
        {
            nuint index = ((nuint)checked((uint)y) * this.stride) + checked((uint)x);

            this.items[index] = value;
        }
    }
}
