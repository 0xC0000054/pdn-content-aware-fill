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

using System;
using System.Runtime.InteropServices;

namespace ContentAwareFill
{
    [System.Security.SuppressUnmanagedCodeSecurity]
    internal static class SafeNativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = false)]
        internal static extern IntPtr HeapAlloc(IntPtr hHeap, uint dwFlags, UIntPtr dwBytes);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool HeapFree(IntPtr hHeap, uint dwFlags, IntPtr lpMem);

        [DllImport("kernel32.dll", SetLastError = false)]
        internal static extern UIntPtr HeapSize(IntPtr hHeap, uint dwFlags, IntPtr lpMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GetProcessHeap();

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr VirtualAlloc(
            IntPtr lpAddress,
            UIntPtr dwSize,
            uint flAllocationType,
            uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool VirtualFree(
            IntPtr lpAddress,
            UIntPtr dwSize,
            uint dwFreeType);
    }
}
