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

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ContentAwareFill.Collections
{
    internal unsafe struct NativeArray<T> : IDisposable where T : unmanaged
    {
        private T* pointer;
        private readonly nuint elementCount;

        public NativeArray(nuint elementCount)
        {
            nuint sizeInBytes = elementCount * (uint)sizeof(T);

            this.pointer = (T*)NativeMemory.Alloc(sizeInBytes);
            this.elementCount = elementCount;

            if (sizeInBytes > 0 && sizeInBytes <= long.MaxValue)
            {
                GC.AddMemoryPressure((long)sizeInBytes);
            }
        }

        public T this[nuint index]
        {
            get
            {
                return *GetAddress(index);
            }
            set
            {
                T* ptr = GetAddress(index);
                *ptr = value;
            }
        }

        public void Dispose()
        {
            T* ptr = this.pointer;
            this.pointer = null;

            if (ptr != null)
            {
                NativeMemory.Free(ptr);

                nuint sizeInBytes = this.elementCount * (uint)sizeof(T);

                if (sizeInBytes > 0 && sizeInBytes <= long.MaxValue)
                {
                    GC.RemoveMemoryPressure((long)sizeInBytes);
                }
            }
        }

        public T* GetAddress(nuint index)
        {
            if (index >= this.elementCount)
            {
                ThrowArgumentOutOfRangeException(index);
            }
            ObjectDisposedException.ThrowIf(this.pointer is null, null);

            return this.pointer + index;
        }

        [DoesNotReturn]
        private void ThrowArgumentOutOfRangeException(nuint index)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index),
                $"{nameof(index)} must be less than {this.elementCount}, actual value: {index}.");
        }
    }
}
