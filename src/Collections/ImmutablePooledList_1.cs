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

using Collections.Pooled;
using PaintDotNet;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ContentAwareFill.Collections
{
    internal sealed class ImmutablePooledList<T> : Disposable, IReadOnlyList<T>, IReadOnlyPooledList<T>
    {
        private readonly PooledList<T> pooledList;

        public static readonly ImmutablePooledList<T> Empty = new([]);

        public ImmutablePooledList(PooledList<T> pooledList)
        {
            this.pooledList = pooledList ?? throw new ArgumentNullException(nameof(pooledList));
        }

        public int Count => this.pooledList.Count;

        public ReadOnlySpan<T> Span => this.pooledList.Span;

        public T this[int index] => this.pooledList[index];

        public IEnumerator<T> GetEnumerator() => this.pooledList.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.pooledList.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
