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

using PaintDotNet.Rendering;
using System;

namespace ContentAwareFill
{
    internal readonly struct PointIndexedBitArray
    {
        private readonly ulong[] blocks;
        private readonly uint stride;

        private PointIndexedBitArray(SizeInt32 size)
        {
            this.stride = checked((uint)size.Width);
            uint height = checked((uint)size.Height);

            ulong area = (ulong)this.stride * height;

            ulong blockCount = area > 64 ? ((area - 1) / 64) + 1 : 1;
            this.blocks = new ulong[checked((int)blockCount)];
        }

        public static PointIndexedBitArray CreateFalse(SizeInt32 size)
        {
            return new PointIndexedBitArray(size);
        }

        public bool this[Point2Int32 target]
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

        public bool GetValue(int x, int y)
        {
            ulong index = ((ulong)checked((uint)y) * this.stride) + checked((uint)x);

            (ulong blockIndex, ulong bitIndex) = Math.DivRem(index, 64);

            ulong segment = this.blocks[blockIndex];

            return (segment & (1UL << (int)bitIndex)) != 0;
        }

        public void SetValue(int x, int y, bool value)
        {
            ulong index = ((ulong)checked((uint)y) * this.stride) + checked((uint)x);

            (ulong blockIndex, ulong bitIndex) = Math.DivRem(index, 64);

            ref ulong segment = ref this.blocks[blockIndex];
            ulong bit = 1UL << (int)bitIndex;

            if (value)
            {
                segment |= bit;
            }
            else
            {
                segment &= ~bit;
            }
        }
    }
}
