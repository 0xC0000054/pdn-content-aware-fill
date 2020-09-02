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

using System.Drawing;

namespace ContentAwareFill
{
    internal sealed class PointIndexedArray<T>
    {
        private T[] items;
        private readonly int stride;

        public PointIndexedArray(int width, int height, T defaultValue)
        {
            items = new T[width * height];
            stride = width;

            for (int i = 0; i < items.Length; i++)
            {
                items[i] = defaultValue;
            }
        }

        public T this[Point target]
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
            int index = (y * stride) + x;

            return items[index];
        }

        public void SetValue(int x, int y, T value)
        {
            int index = (y * stride) + x;

            items[index] = value;
        }
    }
}
