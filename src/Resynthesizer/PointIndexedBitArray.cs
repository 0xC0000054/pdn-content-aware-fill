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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ContentAwareFill
{
    internal readonly struct PointIndexedBitArray
    {
        private readonly ulong[] blocks;
        private readonly int width;
        private readonly int height;

        private PointIndexedBitArray(SizeInt32 size)
        {
            this.width = size.Width;
            this.height = size.Height;

            int area = checked(this.width * this.height);

            int blockCount = area > 64 ? ((area - 1) / 64) + 1 : 1;
            this.blocks = new ulong[blockCount];
        }

        public static PointIndexedBitArray CreateFalse(SizeInt32 size)
        {
            return new PointIndexedBitArray(size);
        }

        public bool this[Point2Int32 target]
        {
            get => GetValue(target.X, target.Y);
            set => SetValue(target.X, target.Y, value);
        }

        public bool GetValue(int x, int y)
        {
            CheckBounds(x, y);

            int index = (y * this.width) + x;

            (int blockIndex, int bitIndex) = Div64Rem(index);

            return (this.blocks[blockIndex] & (1UL << bitIndex)) != 0;
        }

        public void SetValue(int x, int y, bool value)
        {
            CheckBounds(x, y);

            int index = (y * this.width) + x;

            (int blockIndex, int bitIndex) = Div64Rem(index);

            ref ulong segment = ref this.blocks[blockIndex];
            ulong bit = 1UL << bitIndex;

            if (value)
            {
                segment |= bit;
            }
            else
            {
                segment &= ~bit;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (int Quotent, int Remainder) Div64Rem(int number)
        {
            // The uint cast is required for the JIT to optimize the division by 64
            // into a shift right by 6 in assembly.
            // A lot of bit shifting tricks are only valid for unsigned values.
            uint quotent = (uint)number / 64;
            int remainder = number & 63; // Equivalent to number % 64, since 64 is a power of 2.

            return ((int)quotent, remainder);
        }

        [DoesNotReturn]
        private static void ThrowIndexOutOfRangeException()
        {
            throw new IndexOutOfRangeException();
        }

        private void CheckBounds(int x, int y)
        {
            if ((uint)x >= (uint)this.width)
            {
                ThrowIndexOutOfRangeException();
            }

            if ((uint)y >= (uint)this.height)
            {
                ThrowIndexOutOfRangeException();
            }
        }
    }
}
