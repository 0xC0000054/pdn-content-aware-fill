/*
*  This file is part of pdn-content-aware-fill, A Resynthesizer-based
*  content aware fill Effect plug-in for Paint.NET.
*
*  Copyright (C) 2018, 2020, 2021 Nicholas Hayes
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace ContentAwareFill
{
    // This class provides a read-only view of a List<T> without requiring the
    // additional IList<T> indirection of the ReadOnlyCollection<T> class.

    [DebuggerDisplay("Count = {Count}")]
    internal sealed class ReadOnlyList<T> : IEnumerable<T> where T : struct
    {
        private readonly List<T> items;

        public ReadOnlyList(List<T> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            this.items = items;
        }

        public int Count
        {
            get
            {
                return this.items.Count;
            }
        }

        public T this[int index]
        {
            get
            {
                return this.items[index];
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.items.GetEnumerator();
        }
    }
}
