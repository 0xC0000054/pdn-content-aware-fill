/*
*  This file is part of pdn-content-aware-fill, A Resynthesizer-based
*  content aware fill Effect plug-in for Paint.NET.
*
*  Copyright (C) 2018, 2020, 2021, 2022 Nicholas Hayes
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

//#define REPORTLEAKS

using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace ContentAwareFill
{
    /// <summary>
    /// Contains methods for allocating, freeing, and performing operations on memory
    /// that is fixed (pinned) in memory.
    /// </summary>
    internal unsafe static class Memory
    {
        private static IntPtr hHeap;

        private static void CreateHeap()
        {
            hHeap = SafeNativeMethods.GetProcessHeap();

            if (hHeap == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                throw new System.ComponentModel.Win32Exception(error, string.Format(CultureInfo.InvariantCulture, "GetProcessHeap returned NULL, LastError = {0}", error));
            }
        }

        /// <summary>
        /// Allocates a block of memory at least as large as the amount requested.
        /// </summary>
        /// <param name="bytes">The number of bytes you want to allocate.</param>
        /// <param name="zeroFill"><c>true</c> if the memory block should be zero filled; otherwise, <c>false</c>.</param>
        /// <returns>A pointer to a block of memory at least as large as <b>bytes</b>.</returns>
        /// <exception cref="OutOfMemoryException">Thrown if the memory manager could not fulfill the request for a memory block at least as large as <b>bytes</b>.</exception>
        public static IntPtr Allocate(ulong bytes, bool zeroFill)
        {
            if (hHeap == IntPtr.Zero)
            {
                CreateHeap();
            }

            IntPtr block = SafeNativeMethods.HeapAlloc(hHeap, zeroFill ? NativeConstants.HEAP_ZERO_MEMORY : 0, new UIntPtr(bytes));
            if (block == IntPtr.Zero)
            {
                throw new OutOfMemoryException("HeapAlloc returned a null pointer");
            }

            if (bytes > 0)
            {
                GC.AddMemoryPressure((long)bytes);
            }

            return block;
        }

        /// <summary>
        /// Allocates a block of memory at least as large as the amount requested.
        /// </summary>
        /// <param name="bytes">The number of bytes you want to allocate.</param>
        /// <returns>A pointer to a block of memory at least as large as bytes</returns>
        /// <remarks>
        /// This method uses an alternate method for allocating memory (VirtualAlloc in Windows). The allocation
        /// granularity is the page size of the system (usually 4K). Blocks allocated with this method may also
        /// be protected using the ProtectBlock method.
        /// </remarks>
        public static IntPtr AllocateLarge(ulong bytes)
        {
            IntPtr block = SafeNativeMethods.VirtualAlloc(IntPtr.Zero, new UIntPtr(bytes),
                NativeConstants.MEM_COMMIT, NativeConstants.PAGE_READWRITE);

            if (block == IntPtr.Zero)
            {
                throw new OutOfMemoryException("VirtualAlloc returned a null pointer");
            }

            if (bytes > 0)
            {
                GC.AddMemoryPressure((long)bytes);
            }

            return block;
        }

        /// <summary>
        /// Frees a block of memory previously allocated with Allocate().
        /// </summary>
        /// <param name="block">The block to free.</param>
        /// <exception cref="InvalidOperationException">There was an error freeing the block.</exception>
        public static void Free(IntPtr block)
        {
            if (Memory.hHeap != IntPtr.Zero)
            {
                long bytes = (long)SafeNativeMethods.HeapSize(hHeap, 0, block);

                bool result = SafeNativeMethods.HeapFree(hHeap, 0, block);

                if (!result)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new InvalidOperationException("HeapFree returned an error: " + error.ToString(CultureInfo.InvariantCulture));
                }

                if (bytes > 0)
                {
                    GC.RemoveMemoryPressure(bytes);
                }
            }
            else
            {
#if REPORTLEAKS
                throw new InvalidOperationException("memory leak! check the debug output for more info, and http://blogs.msdn.com/ricom/archive/2004/12/10/279612.aspx to track it down");
#endif
            }
        }

        /// <summary>
        /// Frees a block of memory previous allocated with AllocateLarge().
        /// </summary>
        /// <param name="block">The block to free.</param>
        /// <param name="bytes">The size of the block.</param>
        public static void FreeLarge(IntPtr block, ulong bytes)
        {
            bool result = SafeNativeMethods.VirtualFree(block, UIntPtr.Zero, NativeConstants.MEM_RELEASE);

            if (!result)
            {
                int error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException("VirtualFree returned an error: " + error.ToString(CultureInfo.InvariantCulture));
            }

            if (bytes > 0)
            {
                GC.RemoveMemoryPressure((long)bytes);
            }
        }

        /// <summary>
        /// Copies bytes from one area of memory to another. Since this function only
        /// takes pointers, it can not do any bounds checking.
        /// </summary>
        /// <param name="dst">The starting address of where to copy bytes to.</param>
        /// <param name="src">The starting address of where to copy bytes from.</param>
        /// <param name="length">The number of bytes to copy</param>
        public static void Copy(void *dst, void *src, ulong length)
        {
            SafeNativeMethods.memcpy(dst, src, new UIntPtr(length));
        }

        /// <summary>
        /// Sets the memory buffer to zero. Since this function only
        /// takes pointers, it can not do any bounds checking.
        /// </summary>
        /// <param name="dst">The starting address of where to set to zero.</param>
        /// <param name="length">The number of bytes to set to zero.</param>
        public static void SetToZero(void* dst, ulong length)
        {
            SafeNativeMethods.memset(dst, 0, new UIntPtr(length));
        }
    }
}
